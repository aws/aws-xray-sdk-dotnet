using Amazon.XRay.Recorder.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Sampling;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestLambdaContext : TestBase
    {
        private static readonly String _traceHeaderValue = "Root=" + TraceId + ";Parent=53995c3f42cd8ad8;Sampled=1";
        private IAWSXRayRecorder _recorder;

        [TestInitialize]
        public void Initialize()
        {
            _recorder = new AWSXRayRecorder();
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTaskRootKey, "test");
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTraceHeaderKey, _traceHeaderValue);
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTaskRootKey, null);
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTraceHeaderKey, null);
            _recorder.Dispose();
        }

        [TestMethod]
        public void TestSubsegment()
        {
            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)subsegment1.RootSegment;
            _recorder.EndSubsegment();

            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // only subsegment is streamed
            Assert.IsFalse(TraceContext.IsEntityPresent()); // facade segment is cleared from TraceContext
        }


        [TestMethod]
        public void TestGetEntity()
        {
            var entity = TraceContext.GetEntity();

            Assert.AreEqual(entity.GetType(), typeof(FacadeSegment));
            Assert.AreNotEqual(entity.GetType(), typeof(Subsegment));
            Assert.AreNotEqual(entity.GetType(), typeof(Segment));

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)TraceContext.GetEntity();
            Entity facadeSegment = subsegment1.RootSegment;
            _recorder.EndSubsegment();

            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // only subsegment is streamed
            Assert.IsFalse(TraceContext.IsEntityPresent()); // facade segment is cleared from TraceContext
        }


        [TestMethod]
        public void TestNestedSubsegments()
        {
            _recorder.BeginSubsegment("subsegment1");
            Subsegment child = (Subsegment)TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)child.RootSegment;
            _recorder.BeginSubsegment("subsegment2");

            Assert.AreEqual("subsegment2", TraceContext.GetEntity().Name);
            Assert.AreEqual(facadeSegment.TraceId, TraceContext.GetEntity().RootSegment.TraceId); // root segment of subsegment2 is facade segment

            _recorder.EndSubsegment();
            _recorder.EndSubsegment();

            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(child)); // only subsegments are streamed
            Assert.IsFalse(TraceContext.IsEntityPresent());
        }

        [TestMethod]
        public void TestLambdaVariablesNotSetCorrectly()
        {
            String invalidTraceHeader = "Root=" + TraceId + ";Parent=53995c3f42cd8ad8"; // sample decision is missing
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTraceHeaderKey, invalidTraceHeader);

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment = (Subsegment)TraceContext.GetEntity(); // subsegment added with sample decision set to not sampled
            Assert.AreEqual(SampleDecision.NotSampled, subsegment.Sampled);
            _recorder.EndSubsegment(); // subsegment not sampled since invalid TraceHeader value set in the lambda environment

            Assert.IsFalse(TraceContext.IsEntityPresent()); // Facade segment not present in the callcontext 
        }

        [TestMethod]
        public void TestLambdaLeakedSubsegments()
        {
            String secondTraceHeader = "Root=" + Core.Internal.Entities.TraceId.NewId() + ";Parent=53995c3f42cd8ad1;Sampled=1";

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)TraceContext.GetEntity();
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTraceHeaderKey, secondTraceHeader);

            _recorder.BeginSubsegment("subsegment2"); // Environment variables changed, subsegment1 will be dropped
            Subsegment subsegment2 = (Subsegment)TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)subsegment2.RootSegment;

            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // subsegment1 dropped
            Assert.IsTrue(facadeSegment.Subsegments.Contains(subsegment2)); // only subsegment2 is present
            _recorder.EndSubsegment(); // subsegment2 streamed
            Assert.IsFalse(TraceContext.IsEntityPresent()); // Facade segment not present in the callcontext 
        }

        [TestMethod]
        public void TestNoNewSegmentInLambda()
        {
            try
            {
                _recorder.BeginSegment("test");
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                // expected
            }
            Assert.IsFalse(TraceContext.IsEntityPresent()); // Segment cannot be added in Lambda Context
        }
        
        [TestMethod]
        public void TestSegmentEndInLambda()
        {
            try
            {
                _recorder.EndSegment();
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                // expected
            }
        }

        [TestMethod]
        public void TestNotSampledNestedSubsegments()
        {
            String notSampledTraceHeader = "Root=" + Core.Internal.Entities.TraceId.NewId() + ";Parent=53995c3f42cd8ad1;Sampled=0"; //not sampled
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTraceHeaderKey, notSampledTraceHeader);

            _recorder.BeginSubsegment("subsegment1");
            _recorder.BeginSubsegment("subsegment2");
            Subsegment subsegment2 = (Subsegment)TraceContext.GetEntity(); // even if facade segment not sampled, subsegment tree is still available
            _recorder.EndSubsegment();
            Subsegment subsegment1 = (Subsegment)TraceContext.GetEntity();

            Assert.AreEqual("subsegment1", TraceContext.GetEntity().Name);
            FacadeSegment facadeSegment = (FacadeSegment)subsegment1.RootSegment;

            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment2)); // subsegment1 dropped
            Assert.IsTrue(facadeSegment.Subsegments.Contains(subsegment1));
            _recorder.EndSubsegment();

            Assert.IsFalse(TraceContext.IsEntityPresent());
        }
    }
}
