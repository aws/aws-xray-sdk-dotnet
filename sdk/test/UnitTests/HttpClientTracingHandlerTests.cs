using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class HttpClientTracingHandlerTests : TestBase
    {
        private const string URL = "https://httpbin.org/";

        private const string URL404 = "https://httpbin.org/404";

        private readonly HttpClient _httpClient;

        public HttpClientTracingHandlerTests()
        {
            _httpClient = new HttpClient(new HttpClientTracingHandler(new HttpClientHandler()));
        }
        
        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            AWSXRayRecorder.Instance.Dispose();
            _httpClient.Dispose();
        }

        [TestMethod]
        public async Task TestSendAsync()
        {
            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            var request = new HttpRequestMessage(HttpMethod.Get, URL);
            var response = await _httpClient.SendAsync(request);
            
            var segment = TraceContext.GetEntity();
            AWSXRayRecorder.Instance.EndSegment();

            var traceHeader = request.Headers.GetValues(TraceHeader.HeaderKey).FirstOrDefault();
            Assert.IsNotNull(traceHeader);

            var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
            Assert.AreEqual(URL, requestInfo["url"]);
            Assert.AreEqual("GET", requestInfo["method"]);

            var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
            Assert.AreEqual(200, responseInfo["status"]);
            Assert.IsNotNull(responseInfo["content_length"]);
        }

        [TestMethod]
        public async Task Test404SendAsync()
        {
            AWSXRayRecorder.Instance.BeginSegment("parent", TraceId);
            var request = new HttpRequestMessage(HttpMethod.Get, URL404);
            var response = await _httpClient.SendAsync(request);
            
            var segment = TraceContext.GetEntity();
            AWSXRayRecorder.Instance.EndSegment();

            var traceHeader = request.Headers.GetValues(TraceHeader.HeaderKey).FirstOrDefault();
            Assert.IsNotNull(traceHeader);

            var requestInfo = segment.Subsegments[0].Http["request"] as Dictionary<string, object>;
            Assert.AreEqual(URL404, requestInfo["url"]);
            Assert.AreEqual("GET", requestInfo["method"]);

            var responseInfo = segment.Subsegments[0].Http["response"] as Dictionary<string, object>;
            Assert.AreEqual(404, responseInfo["status"]);
        }
    }
}
