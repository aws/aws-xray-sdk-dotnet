//-----------------------------------------------------------------------------
// <copyright file="ConcurrentExceptionTests.cs" company="Amazon.com">
//      Copyright 2026 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.UnitTests
{
    /// <summary>
    /// Tests that recording exceptions on an entity is thread safe and never loses data
    /// (https://github.com/aws/aws-xray-sdk-dotnet/issues/339).
    /// </summary>
    [TestClass]
    public class ConcurrentExceptionTests : TestBase
    {
        private const int Threads = 32;
        private const int Rounds = 50;

        private AWSXRayRecorder _recorder;
        private JsonSegmentMarshaller _marshaller;

        [TestInitialize]
        public void TestInitialize()
        {
            _recorder = new AWSXRayRecorder();
            AWSXRayRecorder.InitializeInstance(recorder: _recorder);
            _marshaller = new JsonSegmentMarshaller();
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            _recorder.Dispose();
            _recorder = null;
        }

        [TestMethod]
        public void TestSequentialAddExceptionKeepsAllExceptions()
        {
            var subsegment = new Subsegment("test");
            var first = new InvalidOperationException("first");
            var second = new ArgumentException("second");

            subsegment.AddException(first);
            subsegment.AddException(second);

            var descriptors = subsegment.Cause.ExceptionDescriptors;
            Assert.AreEqual(2, descriptors.Count);
            Assert.AreSame(first, descriptors[0].Exception);
            Assert.AreSame(second, descriptors[1].Exception);
        }

        [TestMethod]
        public void TestSameExceptionAddedTwiceIsRecordedOnce()
        {
            var subsegment = new Subsegment("test");
            var exception = new InvalidOperationException("only once");

            subsegment.AddException(exception);
            subsegment.AddException(exception);

            Assert.AreEqual(1, subsegment.Cause.ExceptionDescriptors.Count);
        }

        [TestMethod]
        public void TestConcurrentAddExceptionOnSameEntityKeepsAllExceptions()
        {
            for (int round = 0; round < Rounds; round++)
            {
                var subsegment = new Subsegment("test");
                var exceptions = Enumerable.Range(0, Threads).Select(i => new InvalidOperationException("e" + i)).ToArray();

                RunConcurrently(Threads, i => subsegment.AddException(exceptions[i]));

                var recorded = subsegment.Cause.ExceptionDescriptors;
                Assert.AreEqual(Threads, recorded.Count, "round " + round);
                foreach (var e in exceptions)
                {
                    Assert.IsTrue(recorded.Any(d => ReferenceEquals(d.Exception, e)), "missing " + e.Message + " in round " + round);
                }
            }
        }

        [TestMethod]
        public void TestMarshallWhileAddingExceptionConcurrently()
        {
            for (int round = 0; round < Rounds; round++)
            {
                var subsegment = new Subsegment("test");
                subsegment.Id = "1111111111111111";

                // Half the threads add exceptions, the other half serialize the entity at the same time.
                RunConcurrently(Threads, i =>
                {
                    if (i % 2 == 0)
                    {
                        subsegment.AddException(new InvalidOperationException("e" + i));
                    }
                    else
                    {
                        _marshaller.Marshall(subsegment);
                    }
                });

                var json = ParseEntity(_marshaller.Marshall(subsegment));
                Assert.AreEqual(Threads / 2, json["cause"]["exceptions"].Count, "round " + round);
            }
        }

        [TestMethod]
        public void TestMarshallSingleReferenceUsesShortForm()
        {
            var segment = new Segment("parent", TraceId);
            var subsegment = new Subsegment("child");
            segment.AddSubsegment(subsegment);

            var exception = new InvalidOperationException("from child");
            subsegment.AddException(exception);
            segment.AddException(exception); // refers to the exception already recorded in the child

            var json = ParseEntity(_marshaller.Marshall(segment));
            Assert.IsTrue(json["cause"].IsString);
            Assert.AreEqual(subsegment.Cause.ExceptionDescriptors[0].Id, (string)json["cause"]);
        }

        [TestMethod]
        public void TestMarshallReferenceAndExceptionKeepsBoth()
        {
            var segment = new Segment("parent", TraceId);
            var subsegment = new Subsegment("child");
            segment.AddSubsegment(subsegment);

            var fromChild = new InvalidOperationException("from child");
            subsegment.AddException(fromChild);
            segment.AddException(fromChild);                              // reference to the child's exception
            segment.AddException(new ArgumentException("from parent"));  // new exception on the parent

            var json = ParseEntity(_marshaller.Marshall(segment));
            var exceptions = json["cause"]["exceptions"];
            Assert.AreEqual(2, exceptions.Count);

            // The reference only carries the link to the child's exception.
            Assert.AreEqual(subsegment.Cause.ExceptionDescriptors[0].Id, (string)exceptions[0]["cause"]);
            Assert.IsFalse(((System.Collections.IDictionary)exceptions[0]).Contains("id"));

            Assert.AreEqual("from parent", (string)exceptions[1]["message"]);
            Assert.AreEqual("ArgumentException", (string)exceptions[1]["type"]);
        }

        private static JsonData ParseEntity(string marshalled)
        {
            // The first line is the daemon header: {"format":"json","version":1}
            return JsonMapper.ToObject(marshalled.Substring(marshalled.IndexOf('\n') + 1));
        }

        private static void RunConcurrently(int count, Action<int> action)
        {
            var errors = new ConcurrentQueue<Exception>();
            using (var start = new Barrier(count))
            {
                var threads = Enumerable.Range(0, count).Select(i => new Thread(() =>
                {
                    start.SignalAndWait();
                    try
                    {
                        action(i);
                    }
                    catch (Exception e)
                    {
                        errors.Enqueue(e);
                    }
                })).ToList();

                threads.ForEach(t => t.Start());
                threads.ForEach(t => t.Join());
            }

            if (!errors.IsEmpty)
            {
                Assert.Fail(errors.Count + " thread(s) threw, first: " + errors.First());
            }
        }
    }
}
