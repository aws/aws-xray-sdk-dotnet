using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Amazon.XRay.Recorder.SmokeTests
{
    [TestClass]
    public class SmokeTest
    {
        private Mock<ISegmentEmitter> mockEmitter;

        [TestInitialize]
        public void Initialize()
        {
            mockEmitter = new Mock<ISegmentEmitter>();
        }

        [TestMethod]
        public void Emits()
        {
            using (var client = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockEmitter.Object))
            {
                client.BeginSegment("test");
                client.EndSegment();

                mockEmitter.Verify(x => x.Send(It.IsAny<Segment>()), Times.Once);
            }
        }
    }
}
