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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static Amazon.XRay.Recorder.UnitTests.AwsXrayRecorderBuilderTests;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class AwsXrayRecorderTests : TestBase
    {
        private const string DisableXRayTracingKey = "DisableXRayTracing";
        private AWSXRayRecorder _recorder;
#if !NET45
        private XRayOptions _xRayOptions = new XRayOptions();
#endif

        [TestInitialize]
        public void Initialize()
        {
            _recorder = new AWSXRayRecorder();
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
#if NET45

            ConfigurationManager.AppSettings[DisableXRayTracingKey] = string.Empty;
            AppSettings.Reset();
#else
            _xRayOptions = new XRayOptions();
#endif
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, null);
            _recorder.Dispose();
            AWSXRayRecorder.Instance.Dispose();
            _recorder = null;
        }

        [TestMethod]
        public void TestSyncCreateSegmentAndSubsegments()
        {
            _recorder.BeginSegment("parent", TraceId);

            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("child");
            Subsegment child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.EndSubsegment();
            Assert.ReferenceEquals(AWSXRayRecorder.Instance.TraceContext.GetEntity(), parent);

            _recorder.EndSegment();

            Assert.ReferenceEquals(parent, child.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(child));
        }

        [TestMethod]
        public void TestSegmentAndSubsegmentsWithNoTraceId()
        {
            _recorder.BeginSegment("parent");

            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("child");
            Subsegment child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.EndSubsegment();
            Assert.ReferenceEquals(AWSXRayRecorder.Instance.TraceContext.GetEntity(), parent);

            _recorder.EndSegment();

            Assert.AreEqual(SampleDecision.Sampled, parent.Sampled);
            Assert.ReferenceEquals(parent, child.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(child));
        }

        [TestMethod]
        public void TestSegmentAndSubsegmentsWithNullSampleResponse()
        {
            _recorder.BeginSegment("parent", samplingResponse:null);

            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("child");
            Subsegment child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.EndSubsegment();
            Assert.ReferenceEquals(AWSXRayRecorder.Instance.TraceContext.GetEntity(), parent);

            _recorder.EndSegment();

            Assert.ReferenceEquals(parent, child.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(child));
        }

        [TestMethod]
        public async Task TestAsyncCreateSegmentAndSubsegments()
        {
            _recorder.BeginSegment("parent", TraceId);
            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Subsegment child = null;
            await Task.Run(() =>
            {
                _recorder.BeginSubsegment("child");
                child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
                _recorder.EndSubsegment();
            });

            _recorder.EndSegment();

            Assert.IsNotNull(child);
            Assert.ReferenceEquals(parent, child.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(child));
        }

        [TestMethod]
        public void TestAsyncCreateTwoSubsegment()
        {
            _recorder.BeginSegment("parent", TraceId);
            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Subsegment child1 = null;
            Subsegment child2 = null;

            Task task1 = Task.Run(async () =>
            {
                _recorder.BeginSubsegment("child1");
                await Task.Delay(1000);   // Ensure task1 will not complete when task1 is running
                child1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
                _recorder.EndSubsegment();
            });

            Task task2 = Task.Run(() =>
            {
                _recorder.BeginSubsegment("child2");
                child2 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
                _recorder.EndSubsegment();
            });

            Task.WaitAll(task1, task2);

            _recorder.EndSegment();

            Assert.IsNotNull(child1);
            Assert.IsNotNull(child2);
            Assert.ReferenceEquals(parent, child1.Parent);
            Assert.ReferenceEquals(parent, child2.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(child1));
            Assert.IsTrue(parent.Subsegments.Contains(child2));
        }

        [TestMethod]
        public void TestCreateThousandSubsegmentsParallel()
        {
            _recorder.BeginSegment("parent", TraceId);
            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Action action = () =>
            {
                _recorder.BeginSubsegment("child");
                _recorder.EndSubsegment();
            };

            var actions = Enumerable.Repeat(action, 1000).ToArray();
            Parallel.Invoke(actions);

            _recorder.EndSegment();

            Assert.IsTrue(parent.Subsegments.Count < 100);
            Assert.AreEqual(0, parent.Reference);
            Assert.AreEqual(parent.Size, parent.Subsegments.Count);
        }

        [TestMethod]
        public async Task TestAsyncCreateSubsegmentInAChain()
        {
            _recorder.BeginSegment("parent", TraceId);
            var parent = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Subsegment subsegment1 = null;
            Subsegment subsegment2 = null;

            await Task.Run(async () =>
            {
                _recorder.BeginSubsegment("subsegment1");
                subsegment1 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

                await Task.Run(() =>
                {
                    _recorder.BeginSubsegment("subsegment2");
                    subsegment2 = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
                    _recorder.EndSubsegment();
                });

                _recorder.EndSubsegment();
            });

            _recorder.EndSegment();

            Assert.ReferenceEquals(parent, subsegment1.Parent);
            Assert.IsTrue(parent.Subsegments.Contains(subsegment1));
            Assert.ReferenceEquals(subsegment1, subsegment2.Parent);
            Assert.IsTrue(subsegment1.Subsegments.Contains(subsegment2));
        }

        [TestMethod]
        public void TaskAsyncCreateThousandSubsegments()
        {
            _recorder.BeginSegment("parent", TraceId);
            Segment parent = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Action action = () =>
            {
                _recorder.BeginSubsegment("child");
                _recorder.EndSubsegment();
            };

            var tasks = new Task[1000];
            for (int i = 0; i < 1000; i++)
            {
                tasks[i] = Task.Run(action);
            }

            Task.WaitAll(tasks);

            _recorder.EndSegment();

            Assert.IsTrue(parent.Subsegments.Count < 100);
            Assert.AreEqual(0, parent.Reference);
            Assert.AreEqual(parent.Size, parent.Subsegments.Count);
        }

        [TestMethod]
        public async Task TestSubsegmentOutLiveParent()
        {
            var mockEmitter = new Mock<ISegmentEmitter>();

            using (var client = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockEmitter.Object))
            {
                client.BeginSegment("parent", TraceId);
                var parent = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                Subsegment child = null;
                Task task = Task.Run(async () =>
                {
                    client.BeginSubsegment("child");
                    child = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

                    await Task.Delay(1000);    // Wait for parent to end first

                    client.EndSubsegment();
                });

                await Task.Delay(50);    // Wait to ensure subsegment has started
                client.EndSegment();

                // Subsegment is not ended
                mockEmitter.Verify(x => x.Send(It.IsAny<Segment>()), Times.Never);

                Task.WaitAll(task);
                Assert.IsNotNull(child);

                // subsegment ends
                mockEmitter.Verify(x => x.Send(It.IsAny<Segment>()), Times.Once);
            }
        }

        [TestMethod]
        public void TestAddAnnotation()
        {
            _recorder.BeginSegment("test", TraceId);
            _recorder.AddAnnotation("int", 98109);
            _recorder.AddAnnotation("string", "US");
            _recorder.AddAnnotation("bool", true);
            _recorder.AddAnnotation("long", 123L);
            _recorder.AddAnnotation("double", 100.2);

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Assert.AreEqual(98109, segment.Annotations["int"]);
            Assert.AreEqual("US", segment.Annotations["string"]);
            Assert.AreEqual(true, segment.Annotations["bool"]);
            Assert.AreEqual(123L, segment.Annotations["long"]);
            Assert.AreEqual(100.2, segment.Annotations["double"]);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestAddException()
        {
            _recorder.BeginSegment("test", TraceId);
            var e = new ArgumentNullException("value");
            _recorder.AddException(e);

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Assert.IsNotNull(segment);

            Assert.IsTrue(segment.HasFault);
            Assert.ReferenceEquals(e, segment.Cause.ExceptionDescriptors[0].Exception);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestMarkFault()
        {
            _recorder.BeginSegment("test", TraceId);
            _recorder.MarkFault();
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Assert.IsTrue(segment.HasFault);
            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestMarkError()
        {
            _recorder.BeginSegment("test", TraceId);
            _recorder.MarkError();
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            Assert.IsTrue(segment.HasError);
            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestTraceMethodWithReturnValue()
        {
            _recorder.BeginSegment("test", TraceId);

            int count = _recorder.TraceMethod("PlusOneReturn", () => PlusOneReturn(0));
            Assert.AreEqual(1, count);

            var subsegment = AWSXRayRecorder.Instance.TraceContext.GetEntity().Subsegments[0];
            Assert.AreEqual("PlusOneReturn", subsegment.Name);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestTraceMethodReturnVoid()
        {
            _recorder.BeginSegment("test", TraceId);

            int count = 0;
            _recorder.TraceMethod("PlusOneNoReturn", () => PlusOneNoReturn(ref count));
            Assert.AreEqual(1, count);

            var subsegment = AWSXRayRecorder.Instance.TraceContext.GetEntity().Subsegments[0];
            Assert.AreEqual("PlusOneNoReturn", subsegment.Name);

            _recorder.EndSegment();
        }

        [TestMethod]
        public async Task TestTraceMethodAsyncReturnVoid()
        {
            _recorder.BeginSegment("test", TraceId);

            int count = 0;
            await _recorder.TraceMethodAsync("PlusOneNoReturnAsync", () => PlusOneNoReturnAsync<int>(count));

            var subsegment = AWSXRayRecorder.Instance.TraceContext.GetEntity().Subsegments[0];
            Assert.AreEqual("PlusOneNoReturnAsync", subsegment.Name);

            _recorder.EndSegment();
        }

        [TestMethod]
        public async Task TestTraceMethodAsyncWithReturnValueAsync()
        {
            _recorder.BeginSegment("test", TraceId);
            int count = 0;
            var result = await _recorder.TraceMethodAsync("PlusOneReturnAsync", () => PlusOneReturnAsync<int>(count));

            Assert.AreEqual(1, result);

            var subsegment = AWSXRayRecorder.Instance.TraceContext.GetEntity().Subsegments[0];
            Assert.AreEqual("PlusOneReturnAsync", subsegment.Name);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestTraceMethodThrowException()
        {
            _recorder.BeginSegment("test", TraceId);

            try
            {
                _recorder.TraceMethod("exception", () => { throw new ArgumentNullException("value"); });
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                var subsegment = segment.Subsegments[0];
                Assert.IsTrue(subsegment.HasFault);
                Assert.AreEqual("ArgumentNullException", subsegment.Cause.ExceptionDescriptors[0].Type);
            }
            finally
            {
                _recorder.EndSegment();
            }
        }

        [TestMethod]
        public async Task TestTraceMethodAsyncThrowException()
        {
            _recorder.BeginSegment("test", TraceId);

            try
            {
               await  _recorder.TraceMethodAsync("exception", () => PlusOneReturnAsyncThrowException<int>(0));
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                var subsegment = segment.Subsegments[0];
                Assert.IsTrue(subsegment.HasFault);
                Assert.AreEqual("ArgumentNullException", subsegment.Cause.ExceptionDescriptors[0].Type);
            }
            finally
            {
                _recorder.EndSegment();
            }
        }

        [TestMethod]
        public void TestSegmentMissingInTraceContext()
        {
            try
            {
                _recorder.EndSegment();
                Assert.Fail();
            }
            catch (EntityNotAvailableException)
            {
                // Expected;
            }

            try
            {
                _recorder.BeginSubsegment("test");
                Assert.Fail();
            }
            catch (EntityNotAvailableException)
            {
                // Expected;
            }

            try
            {
                _recorder.EndSubsegment();
                Assert.Fail();
            }
            catch (EntityNotAvailableException)
            {
                // Expected;
            }
        }

        [TestMethod]
        public void TestStartSegmentWithNotSampledDecision()
        {
            var mockEmitter = new Mock<ISegmentEmitter>();
            using (var recorder = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockEmitter.Object))
            {
                recorder.BeginSegment("test", TraceId, samplingResponse: new SamplingResponse(SampleDecision.NotSampled));
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

                // BeginSubsegment shouldn't overwrite the segment in trace context
                recorder.BeginSubsegment("sub");
                Assert.ReferenceEquals(segment, AWSXRayRecorder.Instance.TraceContext.GetEntity());

                // EndSubsegment shouldn't release the segment 
                recorder.EndSubsegment();
                Assert.AreEqual(1, segment.Reference);

                recorder.EndSegment();

                mockEmitter.Verify(x => x.Send(It.IsAny<Segment>()), Times.Never);
            }
        }

        [TestMethod]
        public void TestAddHttp()
        {
            _recorder.BeginSegment("test", TraceId);
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.AddHttpInformation("key", "value");

            Assert.AreEqual("value", segment.Http["key"]);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestAddHttpInvalidInput()
        {
            _recorder.BeginSegment("test", TraceId);

            try
            {
                _recorder.AddHttpInformation(null, "value");
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // expected
            }

            try
            {
                _recorder.AddHttpInformation(string.Empty, "value");
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // expected
            }

            try
            {
                _recorder.AddHttpInformation("key", null);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }

        [TestMethod]
        public void TestOverwriteHttpInformationKey()
        {
            _recorder.BeginSegment("TestOverwriteHttpInformationKey", TraceId);
            _recorder.AddHttpInformation("key", "value");
            _recorder.AddHttpInformation("key", "newValue");
            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestSubsegmentStreaming()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);
            var segment = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("first 50");
            for (int i = 0; i < 50; i++)
            {
                _recorder.BeginSubsegment("job" + i);
                _recorder.EndSubsegment();
            }

            _recorder.EndSubsegment();

            _recorder.BeginSubsegment("second 50");
            for (int i = 0; i < 50; i++)
            {
                _recorder.BeginSubsegment("job" + i);
                _recorder.EndSubsegment();
            }

            _recorder.EndSubsegment();

            _recorder.EndSegment();
            Assert.AreEqual(3, segment.Size);
        }

        [TestMethod]
        public void TestSubsegmentStreamingParentSubsegmentDoNotGetRemoved()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);
            var segment = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("parent");
            for (int i = 0; i < 98; i++)
            {
                _recorder.BeginSubsegment("job" + i);
                _recorder.EndSubsegment();
            }

            _recorder.BeginSubsegment("last job");
            var lastJob = (Subsegment)AWSXRayRecorder.Instance.TraceContext.GetEntity();

            // End parent subsegment, and trigger subsegment stream
            AWSXRayRecorder.Instance.TraceContext.SetEntity(lastJob.Parent);
            _recorder.EndSubsegment();

            Assert.AreEqual(2, segment.Size);
            Assert.AreEqual(1, segment.Subsegments.Count);
            Assert.AreEqual(1, segment.Subsegments[0].Subsegments.Count);

            AWSXRayRecorder.Instance.TraceContext.ClearEntity();
        }

        [TestMethod]
        public void TestAddPrecursorIdOnSubsegment()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            _recorder.BeginSubsegment("child");
            var newSegmentId = ThreadSafeRandom.GenerateHexNumber(16);
            _recorder.AddPrecursorId(newSegmentId);
            _recorder.EndSubsegment();

            Assert.AreEqual(newSegmentId, segment.Subsegments[0].PrecursorIds.First());

            _recorder.EndSegment();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestAddInvalidPrecursorId()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);
            _recorder.BeginSubsegment("child");
            _recorder.AddPrecursorId("zzzzzzzzzzzzzzzz");
        }

        [TestMethod]
        public void TestAdd()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);
            _recorder.AddSqlInformation("key1", "value1");
            _recorder.AddSqlInformation("key2", "value2");

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();

            Assert.AreEqual(2, segment.Sql.Count);
            Assert.AreEqual("value1", segment.Sql["key1"]);
            Assert.AreEqual("value2", segment.Sql["key2"]);

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestAddSqlInvalidInput()
        {
            _recorder.BeginSegment(GetType().Name, TraceId);

            try
            {
                _recorder.AddSqlInformation("key", string.Empty);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // expected
            }

            try
            {
                _recorder.AddSqlInformation("key", null);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // expected
            }

            try
            {
                _recorder.AddSqlInformation(null, "value");
                Assert.Fail();
            }
            catch (ArgumentException)
            {
                // expected
            }

            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestOverwriteSqlInformationKey()
        {
            _recorder.BeginSegment("TestOverwriteSqlInformationKey", TraceId);
            _recorder.AddSqlInformation("key", "value");
            _recorder.AddSqlInformation("key", "newValue");
            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestDisableXRayTracingAndNoSegmentSent()
        {
#if NET45
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = "true";
            AppSettings.Reset();
#else
            _xRayOptions.IsXRayTracingDisabled = true;
#endif

            Mock<ISegmentEmitter> mockSegmentEmitter = new Mock<ISegmentEmitter>();
#if NET45
            AppSettings.Reset();
#endif
            using (var recorder = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockSegmentEmitter.Object))
            {
#if !NET45
                recorder.XRayOptions = _xRayOptions;
#endif
                recorder.BeginSegment("test", TraceId);
                var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();    // The segment will be created even if tracing is disabled.

                recorder.BeginSubsegment("test");
                Assert.ReferenceEquals(segment, AWSXRayRecorder.Instance.TraceContext.GetEntity());
                Assert.IsFalse(segment.IsSubsegmentsAdded);

                recorder.EndSubsegment();

                recorder.MarkFault();
                recorder.MarkError();
                recorder.MarkThrottle();
                recorder.AddAnnotation("key", "value");
                recorder.AddHttpInformation("key", "value");
                recorder.AddPrecursorId("id");
                recorder.AddSqlInformation("key", "value");
                recorder.SetNamespace("namespace");
                recorder.TraceMethod("method", () => PlusOneReturn(1));

                recorder.EndSegment();
                mockSegmentEmitter.Verify(x => x.Send(It.IsAny<Segment>()), Times.Never);
            }
        }

        [TestMethod]
        public void TestXrayContext()
        {
            _recorder.BeginSegment("test", TraceId);
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            IDictionary<string, string> xray = (ConcurrentDictionary<string, string>)segment.Aws["xray"];
            var versionText =
                FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(AWSXRayRecorderBuilder)).Location)
                    .ProductVersion;

            Assert.AreEqual(versionText, xray["sdk_version"]);
#if NET45
            Assert.AreEqual("X-Ray for .NET", xray["sdk"]);
#else
            Assert.AreEqual("X-Ray for .NET Core", xray["sdk"]);
#endif
        }

        [TestMethod]
        public void TestServiceContext()
        {
            _recorder.BeginSegment("test", TraceId);
            var segment = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();
#if NET45
            Assert.AreEqual(".NET Framework", segment.Service["runtime"]);
#else
            Assert.AreEqual(".NET Core Framework", segment.Service["runtime"]);
#endif
            Assert.AreEqual(Environment.Version.ToString(), segment.Service["runtime_version"]);
        }

        [TestMethod]
        public void TestNotInitializeSamplingStrategy()
        {
            SamplingInput input = new SamplingInput("randomName", "testPath", "get","test","*");
            _recorder.SamplingStrategy.ShouldTrace(input);
        }

        [TestMethod]
        public void TestAddMetadata()
        {
            _recorder.BeginSegment("metadata", TraceId);
            _recorder.AddMetadata("key1", "value1");
            _recorder.AddMetadata("aws", "key2", "value2");

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("value1", segment.Metadata["default"]["key1"]);
            Assert.AreEqual("value2", segment.Metadata["aws"]["key2"]);
        }

        [TestMethod]
        public void TestUpdateMetadata()
        {
            _recorder.BeginSegment("metadata", TraceId);
            _recorder.AddMetadata("key1", "value1");
            _recorder.AddMetadata("key1", "updated1");

            _recorder.AddMetadata("aws", "key2", "value2");
            _recorder.AddMetadata("aws", "key2", "updated2");

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            _recorder.EndSegment();

            Assert.AreEqual("updated1", segment.Metadata["default"]["key1"]);
            Assert.AreEqual("updated2", segment.Metadata["aws"]["key2"]);
        }

        [TestMethod]
        public void TestSetDaemonAddress()
        {
            string address = "1.2.3.4:56";
            var mockEmitter = new Mock<ISegmentEmitter>();
            using (var recorder = AWSXRayRecorderFactory.CreateAWSXRayRecorder(mockEmitter.Object))
            {
                recorder.SetDaemonAddress(address);
            }

            mockEmitter.Verify(x => x.SetDaemonAddress(address), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotAvailableException))]
        public void TestEndSubsegmentWithSegment()
        {
            _recorder.BeginSegment("segment", TraceId);
            _recorder.EndSubsegment();
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotAvailableException))]
        public void TestStartSubsegmentWithoutSegment()
        {
            _recorder.BeginSubsegment("subsegment");
        }

        [TestMethod]
        [ExpectedException(typeof(EntityNotAvailableException))]

        public void TestEndSegmentWithSubsegment()
        {
            _recorder.BeginSegment("segment", TraceId);
            _recorder.BeginSubsegment("subsegment");
            _recorder.EndSegment();
        }

        [TestMethod]
        public void TestSuppressEntityNotAvailableException()
        {
            string name = StringComparison.InvariantCulture.ToString();
            Assert.IsNotNull(name);
        }

        [TestMethod]
        public void TestDefaultValueOfContextMissingStrategy()
        {
            Assert.AreEqual(ContextMissingStrategy.RUNTIME_ERROR, _recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestLogErrorModeForContextMissingStrategy()
        {
            using (var recorder = new AWSXRayRecorder())
            {
                recorder.ContextMissingStrategy = ContextMissingStrategy.LOG_ERROR;

                recorder.EndSegment();
                recorder.BeginSubsegment("no segment");
                recorder.EndSubsegment();
                recorder.SetNamespace("dummy namespace");
                recorder.AddAnnotation("key", "value");
                recorder.AddHttpInformation("key", "value");
                recorder.MarkError();
                recorder.MarkFault();
                recorder.MarkThrottle();
                recorder.AddException(new ArgumentNullException());
                recorder.AddPrecursorId(Entity.GenerateId());
                recorder.AddSqlInformation("sqlKey", "value");
                recorder.AddMetadata("key", "value");
            }
        }

        [TestMethod]
        public void TestDefaultContextMissingStrategy()
        {
            var recorder = AWSXRayRecorder.Instance;
            Assert.AreEqual(ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestOverrideContextMissingStrategyToRuntimeError()
        {
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, "runtime_error");
            using (var recorder = new AWSXRayRecorder())
            {
                recorder.ContextMissingStrategy = ContextMissingStrategy.LOG_ERROR;
                Assert.AreEqual(ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy);
            }
        }

        [TestMethod]
        public void TestOverrideContextMissingStrategyToLogError()
        {
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, "log_error");
            using (var recorder = new AWSXRayRecorder())
            {
                recorder.ContextMissingStrategy = ContextMissingStrategy.RUNTIME_ERROR;
                Assert.AreEqual(ContextMissingStrategy.LOG_ERROR, recorder.ContextMissingStrategy);
            }
        }

        [TestMethod]
        public void TestDefaultTraceContext()
        {
            var recorder = AWSXRayRecorder.Instance;
#if NET45
            Assert.AreEqual(typeof(CallContextContainer).FullName, recorder.TraceContext.GetType().FullName);
#else
            Assert.AreEqual(typeof(AsyncLocalContextContainer).FullName, recorder.TraceContext.GetType().FullName);
#endif
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorder1()
        {
            AWSXRayRecorder recorder = BuildAWSXRayRecorder(new TestSamplingStrategy());
#if NET45
            AWSXRayRecorder.InitializeInstance(recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: recorder);
#endif
            Assert.AreEqual(AWSXRayRecorder.Instance.SamplingStrategy, recorder.SamplingStrategy); // Custom recorder set in AWSXRayRecorder.Instance.TraceContext
            Assert.AreEqual(typeof(TestSamplingStrategy), recorder.SamplingStrategy.GetType()); // custom strategy set
            Assert.AreEqual(typeof(UdpSegmentEmitter), recorder.Emitter.GetType()); // Default emitter set
            recorder.Dispose();
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorder2() // setting custom daemon address
        {
            string daemonAddress = "udp:127.0.0.2:2001 tcp:127.0.0.1:2000";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            AWSXRayRecorder recorder = BuildAWSXRayRecorder(new TestSamplingStrategy(), daemonAddress: daemonAddress);
#if NET45
            AWSXRayRecorder.InitializeInstance(recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: recorder);
#endif
            Assert.AreEqual(AWSXRayRecorder.Instance.SamplingStrategy, recorder.SamplingStrategy);
            Assert.AreEqual(typeof(TestSamplingStrategy), recorder.SamplingStrategy.GetType()); // custom strategy set
            Assert.AreEqual(typeof(UdpSegmentEmitter), recorder.Emitter.GetType()); // Default emitter set

            var udpEmitter = (UdpSegmentEmitter) recorder.Emitter;
            Assert.AreEqual(expectedUDPEndpoint, udpEmitter.EndPoint);
            recorder.Dispose();
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorder3() // setting custom daemon address to DefaultSamplingStrategy()
        {
            string daemonAddress = "udp:127.0.0.2:2001 tcp:127.0.0.1:2000";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            AWSXRayRecorder recorder = BuildAWSXRayRecorder(daemonAddress: daemonAddress);
#if NET45
            AWSXRayRecorder.InitializeInstance(recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: recorder);
#endif
            Assert.AreEqual(AWSXRayRecorder.Instance.SamplingStrategy, recorder.SamplingStrategy);
            Assert.AreEqual(typeof(DefaultSamplingStrategy), recorder.SamplingStrategy.GetType()); // Default startegy set
            Assert.AreEqual(typeof(UdpSegmentEmitter), recorder.Emitter.GetType()); // Default emitter set

            var udpEmitter = (UdpSegmentEmitter) recorder.Emitter;
            Assert.AreEqual(expectedUDPEndpoint, udpEmitter.EndPoint);

            var defaultStartegy = (DefaultSamplingStrategy) recorder.SamplingStrategy;
            Assert.AreEqual(expectedUDPEndpoint,defaultStartegy.DaemonCfg.UDPEndpoint);
            Assert.AreEqual(expectedTCPEndpoint, defaultStartegy.DaemonCfg.TCPEndpoint);

            recorder.Dispose();
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorde4() // Set custom trace context
        {
            AWSXRayRecorder recorder = BuildAWSXRayRecorder(traceContext:new DummyTraceContext());
#if NET45
            AWSXRayRecorder.InitializeInstance(recorder);
#else
            AWSXRayRecorder.InitializeInstance(recorder: recorder);
#endif
            Assert.AreEqual(typeof(DummyTraceContext), AWSXRayRecorder.Instance.TraceContext.GetType()); // Custom trace context
            recorder.Dispose();
            AWSXRayRecorder.Instance.Dispose();
        }

        [TestMethod]
        public void TestDefaultStreamingStrategyWithDefaultValue()
        {
            IStreamingStrategy defaultStreamingStrategy = new DefaultStreamingStrategy();
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().WithStreamingStrategy(defaultStreamingStrategy).Build();

            Assert.AreEqual(typeof(DefaultStreamingStrategy), recorder.StreamingStrategy.GetType());
            DefaultStreamingStrategy dss = (DefaultStreamingStrategy)recorder.StreamingStrategy;
            Assert.AreEqual(100, dss.MaxSubsegmentSize);
            
        }

        [TestMethod]
        public void TestDefaultStreamingStrategyWithCustomValue()
        {
            IStreamingStrategy defaultStreamingStrategy = new DefaultStreamingStrategy(50);
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().WithStreamingStrategy(defaultStreamingStrategy).Build();
            
            Assert.AreEqual(typeof(DefaultStreamingStrategy), recorder.StreamingStrategy.GetType());
            DefaultStreamingStrategy dss = (DefaultStreamingStrategy)recorder.StreamingStrategy;
            Assert.AreEqual(50, dss.MaxSubsegmentSize);
        }

        [TestMethod]
        public void TestDefaultStreamingStrategyWithNegativeValue()
        {
            Assert.ThrowsException<ArgumentException>(() => new AWSXRayRecorderBuilder().WithStreamingStrategy(new DefaultStreamingStrategy(-100)));
        }

        [TestMethod]
        public void TestBeginSegmentWithCustomTime()
        {
            var custom_time = new DateTime(2020, 1, 13, 21, 18, 47, 228, DateTimeKind.Utc);
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
            recorder.BeginSegment("Segment1", timestamp: custom_time);

            Segment segment = (Segment)recorder.TraceContext.GetEntity();
            Assert.AreEqual(1578950327.228m, segment.StartTime);

            recorder.EndSegment();
        }

        [TestMethod]
        public void TestBeginSubsegmentWithCustomTime()
        {
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
            recorder.BeginSegment("Segment1");
            var custom_time = new DateTime(2019, 07, 14);
            recorder.BeginSubsegment("Subsegment1", custom_time);

            Subsegment subsegment = (Subsegment)recorder.TraceContext.GetEntity();
            Assert.AreEqual(1563062400, subsegment.StartTime);

            recorder.EndSubsegment();
            Assert.IsTrue(DateTime.UtcNow.ToUnixTimeSeconds() >= subsegment.EndTime);
            recorder.EndSegment();
        }

        [TestMethod]
        public void TestEndSubsegmentWithCustomTime()
        {
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().Build();
            recorder.BeginSegment("Segment1");
            recorder.BeginSubsegment("Subsegment1");

            Subsegment subsegment = (Subsegment)recorder.TraceContext.GetEntity();
            Assert.IsTrue(DateTime.UtcNow.ToUnixTimeSeconds() >= subsegment.StartTime);

            var custom_time = new DateTime(2019, 07, 14);
            recorder.EndSubsegment(custom_time);
            Assert.AreEqual(1563062400, subsegment.EndTime);
            recorder.EndSegment();
        }

        public static AWSXRayRecorder BuildAWSXRayRecorder(ISamplingStrategy samplingStrategy = null, ISegmentEmitter segmentEmitter = null, string daemonAddress = null, ITraceContext traceContext = null)
        {
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder();
           
            if (samplingStrategy != null)
            {
                builder.WithSamplingStrategy(samplingStrategy);
            }
            if (segmentEmitter != null)
            {
                builder.WithSegmentEmitter(segmentEmitter);
            }
            if (!string.IsNullOrEmpty(daemonAddress))
            {
                builder.WithDaemonAddress(daemonAddress);
            }
            if(traceContext != null)
            {
                builder.WithTraceContext(traceContext);
            }

            var result = builder.Build();

            return result;
        }

        public class TestSamplingStrategy : ISamplingStrategy
        {
            public SamplingResponse ShouldTrace(SamplingInput input)
            {
                throw new NotImplementedException();
            }
        }

        public class DummyEmitter : ISegmentEmitter
        {
            public void Dispose()
            {
            }

            public void Send(Entity segment)
            {
            }

            public void SetDaemonAddress(string daemonAddress)
            {
            }
        }
        private int PlusOneReturn(int count)
        {
            return count + 1;
        }

        private void PlusOneNoReturn(ref int count)
        {
            count++;
        }
       
        private async Task<int> PlusOneReturnAsync <TResult>(int count)
        {
           await Task.FromResult<int>(count++);
           return count;
        }

        private async Task PlusOneNoReturnAsync<TResult>( int count)
        {
            await Task.FromResult<int>(count++);
        }

        private async Task<int> PlusOneReturnAsyncThrowException<TResult>(int count)
        {
            await Task.FromResult<int>(count++);
            throw new ArgumentNullException("value");
        }
    }
}
