//-----------------------------------------------------------------------------
// <copyright file="AWSSDKHandlerTests.cs" company="Amazon.com">
//      Copyright 2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
//
//      Licensed under the Apache License, Version 2.0 (the "License").
//      You may not use this file except in compliance with the License.
//      A copy of the License is located at
//
//      http://aws.amazon.com/apache2.0
//
//      or in the "license" file accompanying this file. This file is distributed
//      on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
//      express or implied. See the License for the specific language governing
//      permissions and limitations under the License.
// </copyright>
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Amazon.XRay.Recorder.Handlers.AwsSdk.Internal;
using Amazon.XRay.Recorder.UnitTests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class AWSSDKHandlerTests : TestBase
    {
        private const string ManifestKey = "AwsServiceHandlerManifest";

        private AWSXRayRecorder _recorder;

        private static String _path = $"JSONs{Path.DirectorySeparatorChar}AWSRequestInfo.json";
#if !NETFRAMEWORK
        private XRayOptions _xRayOptions = new XRayOptions();
#endif

        [TestInitialize]
        public void TestInitialize()
        {
            _recorder = new AWSXRayRecorder();
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
#if NETFRAMEWORK
            ConfigurationManager.AppSettings[ManifestKey] = null;
            AppSettings.Reset();
#else
            _xRayOptions.AwsServiceHandlerManifest = null;
            _xRayOptions = new XRayOptions();
#endif
            _recorder.Dispose();
            _recorder = null;
            AWSXRayRecorder.Instance.Dispose();
        }

        [TestMethod]
        public async Task TestContextMissingStrategyForAWSSDKHandler()
        {
            AWSXRayRecorder.Instance.ContextMissingStrategy = Core.Strategies.ContextMissingStrategy.LOG_ERROR;
            AWSSDKHandler.RegisterXRayForAllServices();
            var dynamo = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(dynamo);

            AWSXRayRecorder.Instance.BeginSegment("test dynamo", TraceId);
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            AWSXRayRecorder.Instance.EndSegment();
            // The test should not break. No segment is available in the context, however, since the context missing strategy is log error,
            // no exception should be thrown by below code.
            await dynamo.ListTablesAsync();

            Assert.IsNotNull(segment);
        }

        [TestMethod]
        public async Task TestS3SubsegmentNameIsCorrectForAWSSDKHandler()
        {
            AWSSDKHandler.RegisterXRay<IAmazonS3>();
            var s3 = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(s3, null, "TestAmazonId");

            _recorder.BeginSegment("test s3", TraceId);

            await s3.GetObjectAsync("testBucket", "testKey");

            var segment = _recorder.TraceContext.GetEntity();
            _recorder.EndSegment();
            Assert.AreEqual("S3", segment.Subsegments[0].Name);
            Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("version_id"));
            Assert.AreEqual(segment.Subsegments[0].Aws["bucket_name"], "testBucket");
            Assert.AreEqual(segment.Subsegments[0].Aws["operation"], "GetObject");
            Assert.AreEqual(segment.Subsegments[0].Aws["id_2"], "TestAmazonId");
        }

        [TestMethod]
        public async Task TestDynamoDbClient()
        {
            AWSSDKHandler.RegisterXRayForAllServices(_path);
            // IAmazonDynamoDb will be registered. All new instances of AmazonServiceClient will be automatically registered.

            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                string requestId = @"fakerequ-esti-dfak-ereq-uestidfakere";
                CustomResponses.SetResponse(client, requestId);

                _recorder.BeginSegment("test", TraceId);

                await client.ListTablesAsync();

                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                var subsegment = segment.Subsegments[0];
                _recorder.EndSegment();

                Assert.AreEqual(segment.Subsegments.Count, 1);
                Assert.AreEqual(subsegment.Name, "DynamoDBv2");
                Assert.AreEqual(subsegment.Aws["region"], RegionEndpoint.USEast1.SystemName);
                Assert.AreEqual(subsegment.Aws["operation"], "ListTables");
                Assert.AreEqual(requestId, subsegment.Aws["request_id"]);
                Assert.AreEqual("aws", subsegment.Namespace);
            }
        }

        [TestMethod]
        public void TestLoadServiceHandlerManifestWithDefaultConfigurationForAWSSDKHandler()
        {
            var handler = new XRayPipelineHandler(_path);
            Assert.IsNotNull(handler.AWSServiceHandlerManifest);
        }

        [TestMethod]
        public void TestLoadServiceHandlerManifestWithDefaultConfigurationForAWSSDKHandlerNullStream()
        {
            Stream stream = null;
            var handler = new XRayPipelineHandler(stream);
            Assert.IsNotNull(handler.AWSServiceHandlerManifest);
        }

        [TestMethod]
        public void TestLoadServiceHandlerManifestWithDefaultConfigurationForAWSSDKHandlerAsStream()
        {
            using (Stream stream = new FileStream(_path, FileMode.Open, FileAccess.Read))
            {
                var handler = new XRayPipelineHandler(stream);
                Assert.IsNotNull(handler.AWSServiceHandlerManifest);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestLoadServiceInfoManifestInvalidPathForAWSSDKHandler()
        {
            _ = new XRayPipelineHandler(@"IncorrectPath.abc");
        }

        [TestMethod]
        public async Task TestRequestResponseParameterAndDescriptorForAWSSDKHandler()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                CustomResponses.SetResponse(client);
                _recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                var key1 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("1") } };
                var key2 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("2") } };
                var keys = new KeysAndAttributes() { Keys = new List<Dictionary<string, AttributeValue>>() { key1, key2 } };

                await client.BatchGetItemAsync(new Dictionary<string, KeysAndAttributes>() { { "test", keys } });

                _recorder.EndSegment();
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("request_items"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("responses"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("item_count"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("table_names"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("operation"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("request_id"));
            }
        }

        [TestMethod]
        public async Task TestExceptionHandlerAsyncForAWSSDKHandler()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                string requestId = @"fakerequ-esti-dfak-ereq-uestidfakere";
                AmazonServiceException amazonServiceException = new AmazonServiceException();
                amazonServiceException.StatusCode = System.Net.HttpStatusCode.NotFound;
                amazonServiceException.RequestId = requestId;
                CustomResponses.SetResponse(client, (request) => { throw amazonServiceException; });
                _recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                try
                {
                    await client.GetItemAsync(
                          "test", new Dictionary<string, AttributeValue>() { { "invalid_key", new AttributeValue("1") } });
                    Assert.Fail();
                }
                catch (AmazonServiceException e)
                {
                    Assert.ReferenceEquals(e, segment.Subsegments[0].Cause.ExceptionDescriptors[0].Exception);
                    Assert.IsTrue(segment.Subsegments[0].Cause.ExceptionDescriptors[0].Remote); // the exception is remote
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("table_name"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("consistent_read"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("projection_expression"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("attribute_names_substituted"));

                    var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
                    Assert.AreEqual(404, responseInfo["status"]);

                    var subsegment = segment.Subsegments[0];
                    Assert.AreEqual(requestId, subsegment.Aws["request_id"]);
                    Assert.IsTrue(subsegment.HasError);
                    Assert.IsFalse(subsegment.HasFault);

                }
                finally
                {
                    _recorder.EndSegment();
                }
            }
        }

        [TestMethod]
        public async Task TestDynamoSubsegmentNameIsCorrectForAWSSDKHandler()
        {
            var dynamo = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(dynamo);
            _recorder.BeginSegment("test dynamo", TraceId);

            await dynamo.ListTablesAsync();

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("DynamoDBv2", segment.Subsegments[0].Name);
        }

        [TestMethod]
        public async Task TestManifestFileNoLambda() //At this point, current manifest file doen't contain Lambda service.
        {
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(lambda);
            _recorder.BeginSegment("lambda", TraceId);

            await lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = "testFunction"
            });

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();
            Assert.IsFalse(segment.Subsegments[0].Aws.ContainsKey("function_name"));
        }

        [TestMethod]
        public async Task TestLambdaInvokeSubsegmentContainsFunctionNameForAWSSDKHandler()
        {
            String temp_path = $"JSONs{Path.DirectorySeparatorChar}AWSRequestInfoWithLambda.json"; //registering manifest file with Lambda
            AWSSDKHandler.RegisterXRayManifest(temp_path);
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(lambda);
            _recorder.BeginSegment("lambda", TraceId);

            await lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = "testFunction"
            });

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("Invoke", segment.Subsegments[0].Aws["operation"]);
            Assert.AreEqual("testFunction", segment.Subsegments[0].Aws["function_name"]);
        }

        [TestMethod]
        public async Task TestRegisterXRayManifestWithStreamLambdaForAWSSDKHandler()
        {
            String temp_path = $"JSONs{Path.DirectorySeparatorChar}AWSRequestInfoWithLambda.json"; //registering manifest file with Lambda
            using (Stream stream = new FileStream(temp_path, FileMode.Open, FileAccess.Read))
            {
                AWSSDKHandler.RegisterXRayManifest(stream);
            }
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(lambda);
            _recorder.BeginSegment("lambda", TraceId);

            await lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = "testFunction"
            });

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("Invoke", segment.Subsegments[0].Aws["operation"]);
        }

        [TestMethod]
        public void TestSNSSubsegment()
        {
            AWSSDKHandler.RegisterXRay<IAmazonSimpleNotificationService>();
            var sns = new AmazonSimpleNotificationServiceClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(sns);

            _recorder.BeginSegment("test sns", TraceId);

            try
            {
                sns.ListTopicsAsync().Wait();
            }
            catch
            {
                // will throw exception for using anonymous AWS credentials, but will not affect generating traces
            }

            var segment = _recorder.TraceContext.GetEntity();
            _recorder.EndSegment();
            Assert.AreEqual(1, segment.Subsegments.Count);
            Assert.AreEqual("SNS", segment.Subsegments[0].Name); // Name should be SNS instead of SimpleNotificationService
            Assert.AreEqual("ListTopics", segment.Subsegments[0].Aws["operation"]);
            Assert.AreEqual("us-east-1", segment.Subsegments[0].Aws["region"]);
            Assert.AreEqual("aws", segment.Subsegments[0].Namespace);
        }
    }
}
