using Amazon.XRay.Recorder.Core.Internal.Emitters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    public class AWSXRayRecorderFactory
    {
        /// <summary>
        /// Used for unit tests to create a mock implementation. Not intended for production applications.
        /// </summary>
        /// <param name="emitter"></param>
        public static AWSXRayRecorder CreateAWSXRayRecorder(ISegmentEmitter emitter)
        {
            return new AWSXRayRecorder(emitter);
        }
    }
}
