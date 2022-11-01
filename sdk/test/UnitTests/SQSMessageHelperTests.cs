//-----------------------------------------------------------------------------
// <copyright file="AwsXrayRecorderTests.cs" company="Amazon.com">
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

using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Core.Lambda;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class SQSMessageHelperTests : TestBase
    {
        [TestMethod]
        public void TestSyncCreateSegmentAndSubsegments()
        {
            TestTrue("Root=1-632BB806-bd862e3fe1be46a994272793;Sampled=1");
            TestTrue("Root=1-5759e988-bd862e3fe1be46a994272793;Sampled=1");
            TestTrue("Root=1-5759e988-bd862e3fe1be46a994272793;Parent=53995c3f42cd8ad8;Sampled=1");

            TestFalse("Root=1-632BB806-bd862e3fe1be46a994272793;Sampled=0");
            TestFalse("Root=1-5759e988-bd862e3fe1be46a994272793;Sampled=0");
            TestFalse("Root=1-5759e988-bd862e3fe1be46a994272793;Parent=53995c3f42cd8ad8;Sampled=0");
        }

        public static void TestTrue(string traceHeader)
        {
            SQSEvent.SQSMessage sqsMessage = new SQSEvent.SQSMessage();
            sqsMessage.Attributes = new System.Collections.Generic.Dictionary<string, string>();
            sqsMessage.Attributes.Add("AWSTraceHeader", traceHeader);
            Assert.IsTrue(SQSMessageHelper.IsSampled(sqsMessage));
        }

        public static void TestFalse(string traceHeader)
        {
            SQSEvent.SQSMessage sqsMessage = new SQSEvent.SQSMessage();
            sqsMessage.Attributes = new System.Collections.Generic.Dictionary<string, string>();
            sqsMessage.Attributes.Add("AWSTraceHeader", traceHeader);
            Assert.IsFalse (SQSMessageHelper.IsSampled(sqsMessage));
        }
    }
}
