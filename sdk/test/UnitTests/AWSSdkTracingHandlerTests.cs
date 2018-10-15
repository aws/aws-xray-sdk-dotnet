//-----------------------------------------------------------------------------
// <copyright file="AWSSdkTracingHandlerTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Amazon.XRay.Recorder.Handlers.AwsSdk.Internal;
using Amazon.XRay.Recorder.UnitTests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class AWSSdkTracingHandlerTests : TestBase
    {
        private const string ManifestKey = "AwsServiceHandlerManifest";

        private static AWSXRayRecorder _recorder;

        private static String _path = @"JSONs\AWSRequestInfo.json";
#if NET45
        private static AWSSdkTracingHandler _handler;
#else
        private XRayOptions _xRayOptions = new XRayOptions();
#endif

        [TestInitialize]
        public void TestInitialize()
        {
            _recorder = new AWSXRayRecorder();
#if NET45
            _handler = new AWSSdkTracingHandler(_recorder, _path);
            AWSXRayRecorder.InitializeInstance(_recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
# endif
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = null;
            AppSettings.Reset();
#else
            _xRayOptions.AwsServiceHandlerManifest = null;
            _xRayOptions = new XRayOptions();
#endif
            _recorder.Dispose();
            _recorder = null;
        }

#if NET45
        [TestMethod]
        public void TestAddEventHandlerToDynamoDBClient()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                _handler.AddEventHandler(client);
                string requestId = @"fakerequ-esti-dfak-ereq-uestidfakere";
                CustomResponses.SetResponse(client, null, requestId, true);

                _recorder.BeginSegment("test", TraceId);

                client.ListTables();

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
        public void TestLoadServiceHandlerManifestWithDefaultConfiguration()
        {
            var handler = new AWSSdkTracingHandler(AWSXRayRecorder.Instance);
            Assert.IsNotNull(handler.AWSServiceHandlerManifest);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestLoadServiceInfoManifestInvalidPath()
        {
            using (var recorder = new AWSXRayRecorder())
            {
                var handler = new AWSSdkTracingHandler(recorder, @"IncorrectPath.abc");
            }
        }

        [TestMethod]
        public void TestRequestResponseParameterAndDescriptor()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                _handler.AddEventHandler(client);
                CustomResponses.SetResponse(client, null, null, true);

                _recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                var key1 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("1") } };
                var key2 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("2") } };
                var keys = new KeysAndAttributes() { Keys = new List<Dictionary<string, AttributeValue>>() { key1, key2 } };

                client.BatchGetItem(new Dictionary<string, KeysAndAttributes>() { { "test", keys } });

                _recorder.EndSegment();

                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("request_items"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("responses"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("item_count"));
                Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("table_names"));
            }
        }

        [TestMethod]
        public void TestExceptionHandler()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                _handler.AddEventHandler(client);
                CustomResponses.SetResponse(client, (request) => { throw new InvalidOperationException(); });

                _recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                try
                {
                    client.GetItem(
                        "test", new Dictionary<string, AttributeValue>() { { "invalid_key", new AttributeValue("1") } });
                    Assert.Fail();
                }
                catch (InvalidOperationException e)
                {
                    Assert.ReferenceEquals(e, segment.Subsegments[0].Cause.ExceptionDescriptors[0].Exception);
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("table_name"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("consistent_read"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("projection_expression"));
                    Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("attribute_names_substituted"));
                }
                finally
                {
                    _recorder.EndSegment();
                }
            }
        }

        [TestMethod]
        public void TestLambdaInvokeSubsegmentContainsFunctionName()
        {
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            new AWSSdkTracingHandler(AWSXRayRecorder.Instance).AddEventHandler(lambda);
            CustomResponses.SetResponse(lambda, null, null, true);

            _recorder.BeginSegment("lambda", TraceId);

            lambda.Invoke(new InvokeRequest
            {
                FunctionName = "testFunction"
            });

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("Invoke", segment.Subsegments[0].Aws["operation"]);
            Assert.AreEqual("testFunction", segment.Subsegments[0].Aws["function_name"]);
        }

        [TestMethod]
        public void TestDynamoSubsegmentNameIsCorrect()
        {
            var dynamo = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            new AWSSdkTracingHandler(AWSXRayRecorder.Instance).AddEventHandler(dynamo);
            CustomResponses.SetResponse(dynamo, null, null, true);

            _recorder.BeginSegment("test dynamo", TraceId);

            dynamo.ListTables();

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("DynamoDBv2", segment.Subsegments[0].Name);
        }

        [TestMethod]
        public void TestS3SubsegmentNameIsCorrect()
        {
            var s3 = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            new AWSSdkTracingHandler(AWSXRayRecorder.Instance).AddEventHandler(s3);
            CustomResponses.SetResponse(s3, null, null, true);

            _recorder.BeginSegment("test s3", TraceId);

            s3.GetObject("testBucket", "testKey");

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("S3", segment.Subsegments[0].Name);
        }
#endif

        /// <summary>
        ///  Below tests are for <see cref="AWSSDKHandler" /> class (Supports Net45 and Netcore platform).
        /// </summary>

        [TestMethod]
        public void TestS3SubsegmentNameIsCorrectForAWSSDKHandler()
        {
            AWSSDKHandler.RegisterXRay<IAmazonS3>();
            var s3 = new AmazonS3Client(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(s3, null, null, true);

            _recorder.BeginSegment("test s3", TraceId);
#if NET45
            s3.GetObject("testBucket", "testKey");
#else
            s3.GetObjectAsync("testBucket", "testKey").Wait();
#endif
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();
            Assert.AreEqual("S3", segment.Subsegments[0].Name);
            Assert.IsTrue(segment.Subsegments[0].Aws.ContainsKey("version_id"));
            Assert.AreEqual(segment.Subsegments[0].Aws["bucket_name"],"testBucket");
            Assert.AreEqual(segment.Subsegments[0].Aws["operation"], "GetObject");
        }

        [TestMethod]
        public void TestDynamoDbClient()
        {
            AWSSDKHandler.RegisterXRayForAllServices(_path);
            // IAmazonDynamoDb will be registered. All new instances of AmazonServiceClient will be automatically registered.

            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                string requestId = @"fakerequ-esti-dfak-ereq-uestidfakere";
                CustomResponses.SetResponse(client, null, requestId, true);

                _recorder.BeginSegment("test", TraceId);
#if NET45
                client.ListTables();
#else
                client.ListTablesAsync().Wait();
#endif
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
        [ExpectedException(typeof(FileNotFoundException))]
        public void TestLoadServiceInfoManifestInvalidPathForAWSSDKHandler()
        {
            var handler = new XRayPipelineHandler(@"IncorrectPath.abc");
        }

        [TestMethod]
        public void TestRequestResponseParameterAndDescriptorForAWSSDKHandler()
        {
            using (var client = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1))
            {
                CustomResponses.SetResponse(client, null, null, true);
                _recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                var key1 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("1") } };
                var key2 = new Dictionary<string, AttributeValue>() { { "id", new AttributeValue("2") } };
                var keys = new KeysAndAttributes() { Keys = new List<Dictionary<string, AttributeValue>>() { key1, key2 } };

#if NET45
                client.BatchGetItem(new Dictionary<string, KeysAndAttributes>() { { "test", keys } });
#else
                client.BatchGetItemAsync(new Dictionary<string, KeysAndAttributes>() { { "test", keys } }).Wait();
#endif
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
        public void TestDynamoSubsegmentNameIsCorrectForAWSSDKHandler()
        {
            var dynamo = new AmazonDynamoDBClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(dynamo, null, null, true);
            _recorder.BeginSegment("test dynamo", TraceId);
#if NET45
            dynamo.ListTables();
#else
            dynamo.ListTablesAsync().Wait();
#endif
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("DynamoDBv2", segment.Subsegments[0].Name);
        }

        [TestMethod]
        public void TestManifestFileNoLambda() //At this point, current manifest file doen't contain Lambda service.
        {
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(lambda, null, null, true);
            _recorder.BeginSegment("lambda", TraceId);
#if NET45
            lambda.Invoke(new InvokeRequest
            {
                FunctionName = "testFunction"
            });
#else
            lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = "testFunction"
            }).Wait();
#endif
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();
            Assert.IsFalse(segment.Subsegments[0].Aws.ContainsKey("function_name"));
        }

        [TestMethod]
        public void TestLambdaInvokeSubsegmentContainsFunctionNameForAWSSDKHandler()
        {
            String temp_path = @"JSONs\AWSRequestInfoWithLambda.json"; //registering manifest file with Lambda
            AWSSDKHandler.RegisterXRayManifest(temp_path);
            var lambda = new AmazonLambdaClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1);
            CustomResponses.SetResponse(lambda, null, null, true);
            _recorder.BeginSegment("lambda", TraceId);
#if NET45
            lambda.Invoke(new InvokeRequest
            {
                FunctionName = "testFunction"
            });
#else
            lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = "testFunction"
            }).Wait();
#endif
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("Invoke", segment.Subsegments[0].Aws["operation"]);
            Assert.AreEqual("testFunction", segment.Subsegments[0].Aws["function_name"]);
        }
    }
}
