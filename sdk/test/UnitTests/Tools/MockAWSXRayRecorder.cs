using Amazon.XRay.Recorder.Core;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public class MockAWSXRayRecorder : AWSXRayRecorder
    {
        public bool IsTracingDisabledValue { get; set; }
        
        public override bool IsTracingDisabled()
        {
            return IsTracingDisabledValue;
        }
    }
}