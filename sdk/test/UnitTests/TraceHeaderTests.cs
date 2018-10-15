//-----------------------------------------------------------------------------
// <copyright file="TraceHeaderTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TraceHeaderTests : TestBase
    {
        [TestMethod]
        public void TestTryParseWithOnlyRootTraceId()
        {
            string input = "Root=1-5759e988-bd862e3fe1be46a994272793;";
            TraceHeader header;
            Assert.IsTrue(TraceHeader.TryParse(input, out header));
            Assert.AreEqual("1-5759e988-bd862e3fe1be46a994272793", header.RootTraceId);
            Assert.IsNull(header.ParentId);
            Assert.AreEqual(SampleDecision.Unknown, header.Sampled);
        }

        [TestMethod]
        public void TestTrParseWithInvalidTraceId()
        {
            string input = "Root=1-5759e988";
            TraceHeader header;
            Assert.IsFalse(TraceHeader.TryParse(input, out header));
            Assert.IsNull(header);
        }

        [TestMethod]
        public void TestTryParseWithRootParentSampled()
        {
            string input = "Root=1-5759e988-bd862e3fe1be46a994272793; Parent=defdfd9912dc5a56; Sampled=1";
            TraceHeader header;
            Assert.IsTrue(TraceHeader.TryParse(input, out header));

            Assert.AreEqual("1-5759e988-bd862e3fe1be46a994272793", header.RootTraceId);
            Assert.AreEqual("defdfd9912dc5a56", header.ParentId);
            Assert.AreEqual(SampleDecision.Sampled, header.Sampled);
        }

        [TestMethod]
        public void TestTryParseWithInvalidParentId()
        {
            string input = "Root=1-5759e988-bd862e3fe1be46a994272793; Parent=215748";
            TraceHeader header;
            Assert.IsFalse(TraceHeader.TryParse(input, out header));
            Assert.IsNull(header);
        }

        [TestMethod]
        public void TestTryParseWithSampleDisabled()
        {
            string input = "Root=1-5759e988-bd862e3fe1be46a994272793; Sampled=0";
            TraceHeader header;
            Assert.IsTrue(TraceHeader.TryParse(input, out header));

            Assert.AreEqual(SampleDecision.NotSampled, header.Sampled);
        }

        [TestMethod]
        public void TestTryParseSampleRequested()
        {
            string input = "Root=1-5759e988-bd862e3fe1be46a994272793; Sampled=?";
            TraceHeader header;
            Assert.IsTrue(TraceHeader.TryParse(input, out header));
            Assert.AreEqual(SampleDecision.Requested, header.Sampled);
        }

        [TestMethod]
        public void TestTryParseNullString()
        {
            TraceHeader header;
            string nullString = null;
            Assert.IsFalse(TraceHeader.TryParse(nullString, out header));
        }

        [TestMethod]
        public void TestTryParseInvalidString()
        {
            string[] invalidStrings = new string[] { "asdfhdk=fidksldfj", "asdfhdkfidksldfj", ";", ";=", string.Empty };

            foreach (string s in invalidStrings)
            {
                TraceHeader header;
                Assert.IsFalse(TraceHeader.TryParse(s, out header));
            }
        }

        [TestMethod]
        public void TestToStringSampled()
        {
            TraceHeader header = new TraceHeader();
            header.RootTraceId = "1-5759e988-bd862e3fe1be46a994272793";
            header.ParentId = "defdfd9912dc5a56";
            header.Sampled = SampleDecision.Sampled;

            var expected = "Root=1-5759e988-bd862e3fe1be46a994272793; Parent=defdfd9912dc5a56; Sampled=1";

            Assert.AreEqual(expected, header.ToString());
        }

        [TestMethod]
        public void TestToStringNotSampled()
        {
            var header = new TraceHeader();
            header.RootTraceId = "1-5759e988-bd862e3fe1be46a994272793";
            header.ParentId = "defdfd9912dc5a56";
            header.Sampled = SampleDecision.NotSampled;

            var expected = "Root=1-5759e988-bd862e3fe1be46a994272793; Sampled=0";
            Assert.AreEqual(expected, header.ToString());
        }

        [TestMethod]
        public void TestToStringRequested()
        {
            TraceHeader header = new TraceHeader();
            header.RootTraceId = "1-5759e988-bd862e3fe1be46a994272793";
            header.ParentId = "defdfd9912dc5a56";
            header.Sampled = SampleDecision.Requested;

            var expected = "Root=1-5759e988-bd862e3fe1be46a994272793; Parent=defdfd9912dc5a56; Sampled=?";

            Assert.AreEqual(expected, header.ToString());
        }

        [TestMethod]
        public void TestToStringUnknown()
        {
            var header = new TraceHeader();
            header.RootTraceId = "1-5759e988-bd862e3fe1be46a994272793";
            header.ParentId = "defdfd9912dc5a56";
            header.Sampled = SampleDecision.Unknown;

            var expected = "Root=1-5759e988-bd862e3fe1be46a994272793; Parent=defdfd9912dc5a56";
            Assert.AreEqual(expected, header.ToString());
        }

        [TestMethod]
        public void TestTryParseSubsegment()
        {
            using (var recorder = new AWSXRayRecorder())
            {
                recorder.BeginSegment("TraceHeaderTest", TraceId);
                recorder.BeginSubsegment("subjob");
                var subsegment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                TraceHeader header;
                Assert.IsTrue(TraceHeader.TryParse(subsegment, out header));

                Assert.AreEqual(TraceId, header.RootTraceId);
                Assert.AreEqual(subsegment.Id, header.ParentId);
                Assert.AreEqual(SampleDecision.Sampled, header.Sampled);

                recorder.EndSubsegment();
                recorder.EndSegment();
            }
        }
    }
}
