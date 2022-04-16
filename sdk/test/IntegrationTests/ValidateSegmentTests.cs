//-----------------------------------------------------------------------------
// <copyright file="ValidateSegmentTests.cs" company="Amazon.com">
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.IntegrationTests
{
    [TestClass]
    public class ValidateSegmentTests : TestBase
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            TestBase.ClassInit();
        }

        [ClassCleanup]
        public static new void ClassCleanup()
        {
            TestBase.ClassCleanup();
        }

        [TestMethod]
        public async Task TestMinimalSegment()
        {
            var traceId = TraceId.NewId();
            Recorder.BeginSegment(GetType().Name, traceId);
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Thread.Sleep(100);
            Recorder.EndSegment();

            var response = await BatchGetTracesAsync(traceId);

            Assert.IsTrue(response.Traces.Count > 0);

            var segmentJsonData = JsonMapper.ToObject(response.Traces[0].Segments[0].Document);
            Assert.AreEqual(traceId, (string)segmentJsonData["trace_id"]);
            Assert.AreEqual(segment.Id, (string)segmentJsonData["id"]);
            Assert.AreEqual(segment.Name, (string)segmentJsonData["name"]);
            Assert.AreEqual(segment.StartTime.ToString("F5"), ((double)segmentJsonData["start_time"]).ToString("F5"));
            Assert.AreEqual(segment.EndTime.ToString("F5"), ((double)segmentJsonData["end_time"]).ToString("F5"));
        }
        [TestMethod]
        public async Task TestMultipleSubsegments()
        {
            var traceId = TraceId.NewId();
            Recorder.BeginSegment(GetType().Name, traceId);

            for (int i = 0; i < 3; i++)
            {
                Recorder.BeginSubsegment("downstream" + i);
                Recorder.EndSubsegment();
            }

            Dictionary<string, string> subsegmentNames = new Dictionary<string, string>();
            AWSXRayRecorder.Instance.TraceContext.GetEntity().Subsegments.ForEach(x => subsegmentNames[x.Id] = x.Name);

            Recorder.EndSegment();

            var response = await BatchGetTracesAsync(traceId);

            Assert.IsTrue(response.Traces.Count > 0);

            var segmentJsonData = JsonMapper.ToObject(response.Traces[0].Segments[0].Document);

            var subsegments = segmentJsonData["subsegments"];
            Assert.IsNotNull(subsegments);
            Assert.AreEqual(3, subsegments.Count);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual((string)subsegments[i]["name"], (string)subsegmentNames[(string)subsegments[i]["id"]]);
                subsegmentNames.Remove((string)subsegments[i]["id"]);
            }

            Assert.AreEqual(0, subsegmentNames.Count);
        }
        [TestMethod]
        public async Task TestEC2Plugin()
        {
            var mockEC2Plugin = new Mock<IPlugin>();
            IDictionary<string, object> fakeEC2Context = new Dictionary<string, object>();
            fakeEC2Context.Add("instance_id", "i-0ae00afcb550c1164");
            fakeEC2Context.Add("availability_zone", "us-east-1d");
            fakeEC2Context.Add("instance_size", "c4.large");
            fakeEC2Context.Add("ami_id", "ami-a32a7eb4");
            mockEC2Plugin.Setup(x => x.Origin).Returns("AWS::EC2::Instance");
            mockEC2Plugin.Setup(x => x.ServiceName).Returns("ec2");
            mockEC2Plugin.Setup(x => x.TryGetRuntimeContext(out fakeEC2Context)).Returns(true);

            var recorder = new AWSXRayRecorderBuilder().WithPlugin(mockEC2Plugin.Object).Build();
            var traceId = TraceId.NewId();
            recorder.BeginSegment(GetType().Name, traceId);
            Thread.Sleep(100);
            recorder.EndSegment();


            var response = await BatchGetTracesAsync(traceId);

            Assert.IsTrue(response.Traces.Count > 0);

            var segmentJsonData = JsonMapper.ToObject(response.Traces[0].Segments[0].Document);
            Assert.AreEqual("AWS::EC2::Instance", (string)segmentJsonData["origin"]);
            var ec2JsonData = segmentJsonData["aws"]["ec2"];
            Assert.AreEqual("i-0ae00afcb550c1164", (string)ec2JsonData["instance_id"]);
            Assert.AreEqual("us-east-1d", (string)ec2JsonData["availability_zone"]);
            Assert.AreEqual("c4.large", (string)ec2JsonData["instance_size"]);
            Assert.AreEqual("ami-a32a7eb4", (string)ec2JsonData["ami_id"]);
        }

        [TestMethod]
        public async Task TestECSPlugin()
        {
            var mockECSPlugin = new Mock<IPlugin>();
            IDictionary<string, object> fakeECSContext = new Dictionary<string, object>();
            fakeECSContext.Add("container", "localhost");
            mockECSPlugin.Setup(x => x.Origin).Returns("AWS::ECS::Container");
            mockECSPlugin.Setup(x => x.ServiceName).Returns("ecs");
            mockECSPlugin.Setup(x => x.TryGetRuntimeContext(out fakeECSContext)).Returns(true);

            var recorder = new AWSXRayRecorderBuilder().WithPlugin(mockECSPlugin.Object).Build();
            var traceId = TraceId.NewId();
            recorder.BeginSegment(GetType().Name, traceId);
            Thread.Sleep(100);
            recorder.EndSegment();

            var response = await BatchGetTracesAsync(traceId);

            Assert.IsTrue(response.Traces.Count > 0);

            var segmentJsonData = JsonMapper.ToObject(response.Traces[0].Segments[0].Document);
            Assert.AreEqual("AWS::ECS::Container", (string)segmentJsonData["origin"]);
            var ecsJsonData = segmentJsonData["aws"]["ecs"];
            Assert.AreEqual("localhost", (string)ecsJsonData["container"]);
        }

        [TestMethod]
        public async Task TestElasticBeanstalkPlugin()
        {
            var mockEBPlugin = new Mock<IPlugin>();
            IDictionary<string, object> fakeEBContext = new Dictionary<string, object>();
            fakeEBContext.Add("deployment_id", "1");
            fakeEBContext.Add("environment_id", "1");
            fakeEBContext.Add("environment_name", "test");
            fakeEBContext.Add("version_label", "v0");
            mockEBPlugin.Setup(x => x.Origin).Returns("AWS::ElasticBeanstalk::Environment");
            mockEBPlugin.Setup(x => x.ServiceName).Returns("elastic_beanstalk");
            mockEBPlugin.Setup(x => x.TryGetRuntimeContext(out fakeEBContext)).Returns(true);

            var recorder = new AWSXRayRecorderBuilder().WithPlugin(mockEBPlugin.Object).Build();
            var traceId = TraceId.NewId();
            recorder.BeginSegment(GetType().Name, traceId);
            Thread.Sleep(100);
            recorder.EndSegment();

            var response = await BatchGetTracesAsync(traceId);

            Assert.IsTrue(response.Traces.Count > 0);

            var segmentJsonData = JsonMapper.ToObject(response.Traces[0].Segments[0].Document);
            Assert.AreEqual("AWS::ElasticBeanstalk::Environment", (string)segmentJsonData["origin"]);
            var ebJsonData = segmentJsonData["aws"]["elastic_beanstalk"];
            Assert.AreEqual("1", (string)ebJsonData["deployment_id"]);
            Assert.AreEqual("1", (string)ebJsonData["environment_id"]);
            Assert.AreEqual("test", (string)ebJsonData["environment_name"]);
            Assert.AreEqual("v0", (string)ebJsonData["version_label"]);
        }
    }
}
