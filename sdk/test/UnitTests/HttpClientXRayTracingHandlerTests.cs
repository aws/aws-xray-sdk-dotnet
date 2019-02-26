using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class HttpClientXRayTracingHandlerTests : TestBase
    {
        private const string URL = "https://httpbin.org/";

        private const string URL404 = "https://httpbin.org/404";

        private readonly HttpClient _httpClient;

        private static AWSXRayRecorder _recorder;

        public HttpClientXRayTracingHandlerTests()
        {
            _httpClient = new HttpClient(new HttpClientXRayTracingHandler(new HttpClientHandler()));
        }

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
        public async Task TestSendAsync()
        {
            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            var request = new HttpRequestMessage(HttpMethod.Get, URL);
            using(await _httpClient.SendAsync(request)) {}

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            AWSXRayRecorder.Instance.EndSegment();

            var traceHeader = request.Headers.GetValues(TraceHeader.HeaderKey).SingleOrDefault();
            Assert.IsNotNull(traceHeader);

            var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
            Assert.AreEqual(URL, requestInfo["url"]);
            Assert.AreEqual("GET", requestInfo["method"]);

            var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
            Assert.AreEqual(200, responseInfo["status"]);
            Assert.IsNotNull(responseInfo["content_length"]);
        }
        
        /// <summary>
        /// Ensures that when tracing is disabled that HTTP requests can execute as normal. \
        /// See https://github.com/aws/aws-xray-sdk-dotnet/issues/57 for more information. 
        /// </summary>
        [TestMethod]
        public async Task TestSendAsync_XrayDisabled()
        {
            _recorder = new MockAWSXRayRecorder() { IsTracingDisabledValue = true };
#if NET45
            AWSXRayRecorder.InitializeInstance(_recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
# endif
            Assert.IsTrue(AWSXRayRecorder.Instance.IsTracingDisabled());
            
            var request = new HttpRequestMessage(HttpMethod.Get, URL);
            using (var response = await _httpClient.SendAsync(request))
            {
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task Test404SendAsync()
        {
            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            var request = new HttpRequestMessage(HttpMethod.Get, URL404);
            using(await _httpClient.SendAsync(request)) {}

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            AWSXRayRecorder.Instance.EndSegment();

            var traceHeader = request.Headers.GetValues(TraceHeader.HeaderKey).SingleOrDefault();
            Assert.IsNotNull(traceHeader);

            var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
            Assert.AreEqual(URL404, requestInfo["url"]);
            Assert.AreEqual("GET", requestInfo["method"]);

            var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
            Assert.AreEqual(404, responseInfo["status"]);
        }
    }
}
