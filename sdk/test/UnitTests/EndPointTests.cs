using System.Net;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EndPoint = Amazon.XRay.Recorder.Core.Internal.Utils.EndPoint;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EndPointTests
    {
        [TestMethod]
        public void TestWithIP()
        {
            var ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);

            var ep = EndPoint.Of(ip);
            
            Assert.AreEqual(ip, ep.GetIPEndPoint());
        }
        
        [TestMethod]
        public void TestWithHostname()
        {
            var testHost = new HostEndPoint("localhost", 3001);
            var expectedIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);

            var ep = EndPoint.Of(testHost);
            
            Assert.AreEqual(expectedIP, ep.GetIPEndPoint());
        }
    }
}