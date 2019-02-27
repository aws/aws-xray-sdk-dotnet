//-----------------------------------------------------------------------------
// <copyright file="HttpWebRequestTracingExtensionTests.cs" company="Amazon.com">
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
using System.Net;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.System.Net;
using Amazon.XRay.Recorder.UnitTests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class HttpWebRequestTracingExtensionTests : TestBase
    {
        private const string URL = "https://httpbin.org/";

        private const string URL404 = "https://httpbin.org/404";

        private static AWSXRayRecorder _recorder;

        [TestInitialize]
        public void TestInitialize()
        {
            _recorder = new AWSXRayRecorder();
#if NET45
            AWSXRayRecorder.InitializeInstance(_recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
# endif
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            _recorder.Dispose();
            _recorder = null;
        }

        [TestMethod]
        public void TestGetResponseTraced()
        {
            var request = (HttpWebRequest)WebRequest.Create(URL);

            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            using (request.GetResponseTraced())
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                AWSXRayRecorder.Instance.EndSegment();

                Assert.IsNotNull(request.Headers[TraceHeader.HeaderKey]);

                var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
                Assert.AreEqual(URL, requestInfo["url"]);
                Assert.AreEqual("GET", requestInfo["method"]);

                var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
                Assert.AreEqual(200, responseInfo["status"]);
                Assert.IsNotNull(responseInfo["content_length"]);
            }
        }
        
        /// <summary>
        /// Ensures that when tracing is disabled that HTTP requests can execute as normal.
        /// See https://github.com/aws/aws-xray-sdk-dotnet/issues/57 for more information. 
        /// </summary>
        [TestMethod]
        public void TestXrayDisabledGetResponseTraced()
        {
            _recorder = new MockAWSXRayRecorder() { IsTracingDisabledValue = true };
#if NET45
            AWSXRayRecorder.InitializeInstance(_recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
# endif
            Assert.IsTrue(AWSXRayRecorder.Instance.IsTracingDisabled());

            var request = (HttpWebRequest)WebRequest.Create(URL);
            
            using (var response = request.GetResponseTraced() as HttpWebResponse)
            {
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetAsyncResponseTraced()
        {
            var request = (HttpWebRequest)WebRequest.Create(URL);

            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            using (await request.GetAsyncResponseTraced())
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                AWSXRayRecorder.Instance.EndSegment();

                Assert.IsNotNull(request.Headers[TraceHeader.HeaderKey]);

                var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
                Assert.AreEqual(URL, requestInfo["url"]);
                Assert.AreEqual("GET", requestInfo["method"]);

                var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
                Assert.AreEqual(200, responseInfo["status"]);
                Assert.IsNotNull(responseInfo["content_length"]);
            }
        }
        
        /// <summary>
        /// Ensures that when tracing is disabled that HTTP requests can execute as normal.
        /// See https://github.com/aws/aws-xray-sdk-dotnet/issues/57 for more information. 
        /// </summary>
        [TestMethod]
        public async Task TestXrayDisabledGetAsyncResponseTraced()
        {
            _recorder = new MockAWSXRayRecorder() { IsTracingDisabledValue = true };
#if NET45
            AWSXRayRecorder.InitializeInstance(_recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
# endif
            Assert.IsTrue(AWSXRayRecorder.Instance.IsTracingDisabled());

            var request = (HttpWebRequest)WebRequest.Create(URL);
            
            using (var response = await request.GetAsyncResponseTraced() as HttpWebResponse)
            {
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

#if !NET45
        [TestMethod]
        public void TestExceptionGetResponseTraced()
        {
            var request = (HttpWebRequest)WebRequest.Create(URL404);

            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            try
            {
                using (request.GetResponseTraced()) {}
                Assert.Fail();
            }

            catch (WebException) //expected
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                AWSXRayRecorder.Instance.EndSegment();

                Assert.IsNotNull(request.Headers[TraceHeader.HeaderKey]);

                var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
                Assert.AreEqual(URL404, requestInfo["url"]);
                Assert.AreEqual("GET", requestInfo["method"]);

                var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
                Assert.AreEqual(404, responseInfo["status"]);
                Assert.IsNotNull(responseInfo["content_length"]);

                var subsegment = segment.Subsegments[0];
                Assert.IsTrue(subsegment.HasError);
                Assert.IsFalse(subsegment.HasFault);
            }
        }

        [TestMethod]
        public async Task TestExceptionGetAsyncResponseTraced()
        {
            var request = (HttpWebRequest)WebRequest.Create(URL404);

            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            try
            {
                using (await request.GetAsyncResponseTraced()) {}
                Assert.Fail();
            }

            catch (WebException) //expected
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                AWSXRayRecorder.Instance.EndSegment();

                Assert.IsNotNull(request.Headers[TraceHeader.HeaderKey]);

                var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
                Assert.AreEqual(URL404, requestInfo["url"]);
                Assert.AreEqual("GET", requestInfo["method"]);

                var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
                Assert.AreEqual(404, responseInfo["status"]);
                Assert.IsNotNull(responseInfo["content_length"]);

                var subsegment = segment.Subsegments[0];
                Assert.IsTrue(subsegment.HasError);
                Assert.IsFalse(subsegment.HasFault);
            }
        }
#endif
    }
}
