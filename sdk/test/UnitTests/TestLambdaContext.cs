//-----------------------------------------------------------------------------
// <copyright file="TestLambdaContext.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using System.Net;
using Amazon.XRay.Recorder.Handlers.System.Net;
using Moq;
using Amazon.XRay.Recorder.Core.Internal.Emitters;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestLambdaContext : TestBase
    {
        public const string LambdaTraceHeaderKey = "_X_AMZN_TRACE_ID";
        private const string MOCK_URL = "https://httpbin.org/";
        private static readonly String _traceHeaderValue = "Root=" + TraceId + ";Parent=53995c3f42cd8ad8;Sampled=1";
        private AWSXRayRecorder _recorder;

        [TestInitialize]
        public void Initialize()
        {
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTaskRootKey, "test");
            Environment.SetEnvironmentVariable(LambdaTraceHeaderKey, _traceHeaderValue);
            _recorder = new AWSXRayRecorderBuilder().Build();
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            Environment.SetEnvironmentVariable(AWSXRayRecorder.LambdaTaskRootKey, null);
            Environment.SetEnvironmentVariable(LambdaTraceHeaderKey, null);
        }

        [TestMethod]
        public void TestLamdaSampled()
        {
            var mockEmitter = new Mock<ISegmentEmitter>();

            using (AWSXRayRecorder recorder = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockEmitter.Object))
            {
                AWSXRayRecorder.InitializeInstance(recorder: _recorder);

                TestLambdaHelper(recorder, true);
                mockEmitter.Verify(x => x.Send(It.IsAny<Subsegment>()), Times.Exactly(2));

                TestLambdaHelper(recorder, false);
                mockEmitter.Verify(x => x.Send(It.IsAny<Subsegment>()), Times.Exactly(2));

                TestLambdaHelper(recorder, true);
                mockEmitter.Verify(x => x.Send(It.IsAny<Subsegment>()), Times.Exactly(4));

                TestLambdaHelper(recorder, false);
                mockEmitter.Verify(x => x.Send(It.IsAny<Subsegment>()), Times.Exactly(4));
            }
        }

        private static void TestLambdaHelper(AWSXRayRecorder recorder, bool sampled)
        {
            if (sampled)
            {
                recorder.BeginSubsegment("subsegment1");
            }
            else
            {
                recorder.BeginSubsegmentWithoutSampling("subsegment1");
            }

            var request = (HttpWebRequest)WebRequest.Create(MOCK_URL);

            using (var response = request.GetResponseTraced() as HttpWebResponse)
            {
                Assert.IsNotNull(response);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                string header = request.Headers.Get(TraceHeader.HeaderKey);
                Assert.AreEqual(sampled ? SampleDecision.Sampled : SampleDecision.NotSampled, TraceHeader.FromString(header).Sampled);
            }

            recorder.EndSubsegment();
        }

        [TestMethod]
        public void TestSubsegment()
        {
            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)subsegment1.RootSegment;
            _recorder.EndSubsegment();
            Assert.AreEqual(typeof(LambdaContextContainer), AWSXRayRecorder.Instance.TraceContext.GetType());
            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // only subsegment is streamed
            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()); // facade segment is cleared from AWSXRayRecorder.Instance.TraceContext
        }


        [TestMethod]
        public void TestGetEntity()
        {
            var entity = _recorder.TraceContext.GetEntity();

            Assert.AreEqual(entity.GetType(), typeof(FacadeSegment));
            Assert.AreNotEqual(entity.GetType(), typeof(Subsegment));
            Assert.AreNotEqual(entity.GetType(), typeof(Segment));

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Entity facadeSegment = subsegment1.RootSegment;
            _recorder.EndSubsegment();

            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // only subsegment is streamed
            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()); // facade segment is cleared from AWSXRayRecorder.Instance.TraceContext
            Assert.AreEqual(typeof(LambdaContextContainer), AWSXRayRecorder.Instance.TraceContext.GetType());
        }


        [TestMethod]
        public void TestNestedSubsegments()
        {
            _recorder.BeginSubsegment("subsegment1");
            Subsegment child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)child.RootSegment;
            _recorder.BeginSubsegment("subsegment2");

            Assert.AreEqual("subsegment2", AWSXRayRecorder.Instance.TraceContext.GetEntity().Name);
            Assert.AreEqual(facadeSegment.TraceId, AWSXRayRecorder.Instance.TraceContext.GetEntity().RootSegment.TraceId); // root segment of subsegment2 is facade segment

            _recorder.EndSubsegment();
            _recorder.EndSubsegment();

            Assert.AreEqual(facadeSegment.GetType(), typeof(FacadeSegment));
            Assert.IsFalse(facadeSegment.Subsegments.Contains(child)); // only subsegments are streamed
            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent());
        }

        [TestMethod]
        public void TestLambdaVariablesNotSetCorrectly()
        {
            String invalidTraceHeader = "Root=" + TraceId + ";Parent=53995c3f42cd8ad8"; // sample decision is missing
            Environment.SetEnvironmentVariable(LambdaTraceHeaderKey, invalidTraceHeader);

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity(); // subsegment added with sample decision set to not sampled
            Assert.AreEqual(SampleDecision.NotSampled, subsegment.Sampled);
            _recorder.EndSubsegment(); // subsegment not sampled since invalid TraceHeader value set in the lambda environment

            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()); // Facade segment not present in the callcontext 
        }

        [TestMethod]
        public void TestLambdaLeakedSubsegments()
        {
            String secondTraceHeader = "Root=" + Core.Internal.Entities.TraceId.NewId() + ";Parent=53995c3f42cd8ad1;Sampled=1";

            _recorder.BeginSubsegment("subsegment1");
            Subsegment subsegment1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Environment.SetEnvironmentVariable(LambdaTraceHeaderKey, secondTraceHeader);

            _recorder.BeginSubsegment("subsegment2"); // Environment variables changed, subsegment1 will be dropped
            Subsegment subsegment2 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            FacadeSegment facadeSegment = (FacadeSegment)subsegment2.RootSegment;

            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment1)); // subsegment1 dropped
            Assert.IsTrue(facadeSegment.Subsegments.Contains(subsegment2)); // only subsegment2 is present
            _recorder.EndSubsegment(); // subsegment2 streamed
            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()); // Facade segment not present in the callcontext 
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
            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()); // Segment cannot be added in Lambda Context
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
            Environment.SetEnvironmentVariable(LambdaTraceHeaderKey, notSampledTraceHeader);

            _recorder.BeginSubsegment("subsegment1");
            _recorder.BeginSubsegment("subsegment2");
            Subsegment subsegment2 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity(); // even if facade segment not sampled, subsegment tree is still available
            _recorder.EndSubsegment();
            Subsegment subsegment1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Assert.AreEqual("subsegment1", AWSXRayRecorder.Instance.TraceContext.GetEntity().Name);
            FacadeSegment facadeSegment = (FacadeSegment)subsegment1.RootSegment;

            Assert.IsFalse(facadeSegment.Subsegments.Contains(subsegment2)); // subsegment1 dropped
            Assert.IsTrue(facadeSegment.Subsegments.Contains(subsegment1));
            _recorder.EndSubsegment();

            Assert.IsFalse(AWSXRayRecorder.Instance.TraceContext.IsEntityPresent());
        }

        [TestMethod]
        public void TestBeginSubsegmentWithCustomTime()
        {
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
            
            var custom_time = new DateTime(2019, 07, 14);
            recorder.BeginSubsegment("Subsegment1", custom_time);

            Subsegment subsegment = (Subsegment)recorder.TraceContext.GetEntity();
            Assert.AreEqual(1563062400, subsegment.StartTime);

            recorder.EndSubsegment();
            Assert.IsTrue(DateTime.UtcNow.ToUnixTimeSeconds() >= subsegment.EndTime);
        }

        [TestMethod]
        public void TestEndSubsegmentWithCustomTime()
        {
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
            recorder.BeginSubsegment("Subsegment1");

            Subsegment subsegment = (Subsegment)recorder.TraceContext.GetEntity();
            Assert.IsTrue(DateTime.UtcNow.ToUnixTimeSeconds() >= subsegment.StartTime);

            var custom_time = new DateTime(2019, 07, 14);
            recorder.EndSubsegment(custom_time);
            Assert.AreEqual(1563062400, subsegment.EndTime);
        }
    }
}
