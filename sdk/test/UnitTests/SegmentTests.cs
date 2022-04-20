//-----------------------------------------------------------------------------
// <copyright file="SegmentTests.cs" company="Amazon.com">
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class SegmentTests : TestBase
    {
        private string[] _validTraceIds = new string[]
        {
                                        "1-5759e988-bd862e3fe1be46a994272793",
                                        "1-5759E988-BD862E3FE1BE46A994272793"
                                        };

        private string[] _invalidTraceIds = new string[]
        {
                                        string.Empty,
                                        "2-5759e988-bd862e3fe1be46a994272793",
                                        "1-5759e988a-bd862e3fe1be46a99427279",
                                        "1-5759e988-bd862e3fe1be46a994272793a",
                                        "1-5759e988-bd862e3fe1be46a99427khkj",
                                        "1-xxxxxxxx-bd862e3fe1be46a994272793",
                                        "a-5759e988-bd862e3fe1be46a994272793",
                                        "1*5759e988,bd862e3fe1be46a994272793",
                                        "1-5759e9881111-bd862e3fe1be46a99427",
                                        "1-5759e-bd862e3fe1be46a994272111793",
                                        "1-HJKLEIUR-bd862e3fe1be46a994272793",
                                        "1-.759e988-bd862e3fe1be46a994272793"
                                        };

        private string[] _invalidSegmentIds = new string[]
        {
                                        string.Empty,
                                        "123",
                                        "1234567890",
                                        "abcdefgh",
                                        ",./!@#$%"
        };

        [TestMethod]
        public void TestCreateSubsegmentWithName()
        {
            var subsegment = new Subsegment("test");

            Assert.AreEqual(subsegment.Name, "test");
            Assert.IsTrue(Entity.IsIdValid(subsegment.Id));
            Assert.IsNull(subsegment.TraceId);
            Assert.IsNull(subsegment.ParentId);
            Assert.IsFalse(subsegment.IsSubsegmentsAdded);
        }

        [TestMethod]
        public void TestCreateSegmentWithInvalidParentId()
        {
            foreach (string id in _invalidSegmentIds)
            {
                try
                {
                    var segment = new Segment("test", null, id);
                    Assert.Fail();
                }
                catch (ArgumentException)
                {
                }
            }
        }

        [TestMethod]
        public void TestCreateSegmentWithValidTraceId()
        {
            try
            {
                foreach (string id in _validTraceIds)
                {
                    var segment = new Segment("test", id);
                }
            }
            catch (ArgumentException ex)
            {
                Assert.Fail("Expected no exception, but got: " + ex.Message);
            }
        }

        [TestMethod]
        public void TestCreateSegmentWithInvalidTraceId()
        {
            foreach (string id in _invalidTraceIds)
            {
                try
                {
                    var segment = new Segment(id, "test");

                    // If an exception is not thrown with invalid id, fail the test
                    Assert.Fail();
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateSegmentWithInvalidNameTest()
        {
            _ = new Segment(null, TraceId);
        }

        [TestMethod]
        public void TestSegmentIdIsValid()
        {
            var segment = new Segment("test", _validTraceIds[0]);
            string id = segment.Id;

            Assert.AreEqual(id.Length, 16);
            Assert.IsTrue(long.TryParse(id, NumberStyles.HexNumber, null, out _));
        }

        [TestMethod]
        public void TestAddSegment()
        {
            var parent = new Segment("parent", TraceId);
            var child = new Subsegment("child");

            parent.AddSubsegment(child);

            Assert.ReferenceEquals(child.Parent, parent);
            Assert.IsTrue(parent.Subsegments.Contains(child));
        }

        [TestMethod]
        public void TestReferenceCountWithOneSegment()
        {
            var segment = new Segment("test", TraceId);

            Assert.AreEqual(segment.Reference, 1);
            Assert.IsFalse(segment.IsEmittable());

            segment.Release();
            Assert.AreEqual(segment.Reference, 0);
            Assert.IsTrue(segment.IsEmittable());
        }

        [TestMethod]
        public void TestAddRefAndReleaseWithSubsegment()
        {
            var parent = new Segment("parent", TraceId);
            var child = new Subsegment("child");
            parent.AddSubsegment(child);

            Assert.AreEqual(parent.Reference, 2);
            Assert.AreEqual(child.Reference, 1);

            child.Release();
            Assert.AreEqual(parent.Reference, 1);
            Assert.AreEqual(child.Reference, 0);
            Assert.IsFalse(parent.IsEmittable());
            Assert.IsFalse(child.IsEmittable());

            parent.Release();
            Assert.AreEqual(parent.Reference, 0);
            Assert.IsTrue(parent.IsEmittable());
            Assert.IsTrue(child.IsEmittable());
        }

        [TestMethod]
        public void TestAddRefAndReleaseWithSubsegmentInReverseOrder()
        {
            var parent = new Segment("parent", TraceId);
            var child = new Subsegment("child");
            parent.AddSubsegment(child);

            Assert.AreEqual(parent.Reference, 2);
            Assert.AreEqual(child.Reference, 1);

            parent.Release();
            Assert.AreEqual(parent.Reference, 1);
            Assert.AreEqual(child.Reference, 1);
            Assert.IsFalse(parent.IsEmittable());
            Assert.IsFalse(child.IsEmittable());

            child.Release();
            Assert.AreEqual(parent.Reference, 0);
            Assert.AreEqual(child.Reference, 0);
            Assert.IsTrue(parent.IsEmittable());
            Assert.IsTrue(child.IsEmittable());
        }

        [TestMethod]
        public void TestAddRefAndReleaseWithTwoSubsegment()
        {
            var s1 = new Segment("s1", TraceId);
            var s21 = new Subsegment("s21");
            var s22 = new Subsegment("s22");

            s1.AddSubsegment(s21);
            s1.AddSubsegment(s22);

            Assert.AreEqual(s1.Reference, 3);
            Assert.AreEqual(s21.Reference, 1);
            Assert.AreEqual(s22.Reference, 1);

            s21.Release();
            Assert.AreEqual(s1.Reference, 2);
            Assert.AreEqual(s21.Reference, 0);
            Assert.AreEqual(s22.Reference, 1);
            Assert.IsFalse(s1.IsEmittable());
            Assert.IsFalse(s21.IsEmittable());
            Assert.IsFalse(s22.IsEmittable());

            s22.Release();
            Assert.AreEqual(s1.Reference, 1);
            Assert.AreEqual(s22.Reference, 0);
            Assert.IsFalse(s1.IsEmittable());
            Assert.IsFalse(s22.IsEmittable());

            s1.Release();
            Assert.AreEqual(s1.Reference, 0);
            Assert.IsTrue(s1.IsEmittable());
            Assert.IsTrue(s21.IsEmittable());
            Assert.IsTrue(s22.IsEmittable());
        }

        [TestMethod]
        public void TestAddRefAndReleaseWithThreeSegment()
        {
            var s1 = new Segment("s1", TraceId);
            var s2 = new Subsegment("s2");
            var s3 = new Subsegment("s3");

            s1.AddSubsegment(s2);
            s2.AddSubsegment(s3);

            Assert.AreEqual(s1.Reference, 2);
            Assert.AreEqual(s2.Reference, 2);
            Assert.AreEqual(s3.Reference, 1);

            s2.Release();
            Assert.AreEqual(s1.Reference, 2);
            Assert.AreEqual(s2.Reference, 1);
            Assert.AreEqual(s3.Reference, 1);
            Assert.IsFalse(s1.IsEmittable());
            Assert.IsFalse(s2.IsEmittable());
            Assert.IsFalse(s3.IsEmittable());

            s3.Release();
            Assert.AreEqual(s1.Reference, 1);
            Assert.AreEqual(s2.Reference, 0);
            Assert.AreEqual(s3.Reference, 0);
            Assert.IsFalse(s1.IsEmittable());
            Assert.IsFalse(s2.IsEmittable());
            Assert.IsFalse(s3.IsEmittable());

            s1.Release();
            Assert.AreEqual(s1.Reference, 0);
            Assert.IsTrue(s1.IsEmittable());
            Assert.IsTrue(s2.IsEmittable());
            Assert.IsTrue(s3.IsEmittable());
        }

        [TestMethod]
        public void TestAddException()
        {
            var segment = new Segment("test", TraceId);

            var e = new EntityNotAvailableException("Test someting wrong happens");
            segment.AddException(e);

            Assert.IsTrue(segment.HasFault);
            Assert.IsFalse(segment.HasError);
            Assert.IsNotNull(segment.Cause);
            var descriptor = segment.Cause.ExceptionDescriptors[0];
            Assert.AreEqual("Test someting wrong happens", descriptor.Message);
            Assert.AreEqual("EntityNotAvailableException", descriptor.Type);
            Assert.ReferenceEquals(e, descriptor.Exception);
        }

        [TestMethod]
        public void TestHttpOverwriteValue()
        {
            var segment = new Segment("test", TraceId);
            segment.Http["key"] = "value1";
            segment.Http["key"] = "value2";

            Assert.AreEqual("value2", segment.Http["key"]);
        }


#if NET45
        [TestMethod]
        public void TestSegmentIsSerializable()
        {
            string segmentName = "test";
            string parentId = Entity.GenerateId();
            Segment segment = new Segment(segmentName, TraceId, parentId);

            Exception ex = new ArgumentException("test can't be null");
            segment.AddException(ex);

            segment.SetStartTimeToNow();
            segment.SetEndTimeToNow();

            string serviceKey = "key1";
            string serviceValue = "value1";
            segment.Service[serviceKey] = serviceValue;

            Segment segmentAfterSerialize = (Segment)SerializeAndDeserialize(segment);

            Assert.AreEqual(segmentName, segmentAfterSerialize.Name);
            Assert.AreEqual(TraceId, segmentAfterSerialize.TraceId);
            Assert.AreEqual(parentId, segmentAfterSerialize.ParentId);
            Assert.AreEqual(ex.Message, segmentAfterSerialize.Cause.ExceptionDescriptors[0].Message);
            Assert.AreEqual(segmentAfterSerialize.StartTime, segment.StartTime);
            Assert.AreEqual(segmentAfterSerialize.EndTime, segment.EndTime);
            Assert.AreEqual(segmentAfterSerialize.Service[serviceKey], segment.Service[serviceKey]);
        }

        [TestMethod]
        public void TestSubsegmentIsSerializable()
        {
            Subsegment subsegment = new Subsegment("test");
            string testNamespace = "namespace";
            subsegment.Namespace = testNamespace;

            Entity parent = new Segment("parent", TraceId);
            subsegment.Parent = parent;

            subsegment.HasStreamed = true;

            string type = "type1";
            subsegment.Type = type;

            string precursorId = Entity.GenerateId();
            subsegment.AddPrecursorId(precursorId);

            Subsegment subsegmentAfterSerialize = (Subsegment)SerializeAndDeserialize(subsegment);

            Assert.AreEqual(testNamespace, subsegmentAfterSerialize.Namespace);
            Assert.AreEqual(parent.TraceId, subsegmentAfterSerialize.Parent.TraceId);
            Assert.IsTrue(subsegmentAfterSerialize.HasStreamed);
            Assert.AreEqual(type, subsegmentAfterSerialize.Type);
            Assert.AreEqual(precursorId, subsegmentAfterSerialize.PrecursorIds.First());
        }
#endif

        private static object SerializeAndDeserialize(Object source)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            stream.Position = 0;
            object obj = formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        [TestMethod]
        public void TestSetUser()
        {
            var segment = new Segment("SegmentA");
            segment.SetUser("UserA");

            Assert.AreEqual("UserA", segment.GetUser());
        }

        [TestMethod]
        public void TestSetUserWithNullValue()
        {
            var segment = new Segment("SegmentA");

            Assert.ThrowsException<ArgumentNullException>(() => segment.SetUser(null));
        }

        [TestMethod]
        public void TestSetUserWhenSegmentAlreadyStreamed()
        {
            var segment = new Segment("SegmentA");
            segment.HasStreamed = true;

            Assert.ThrowsException<AlreadyEmittedException>(() => segment.SetUser("UserA"), "Segment SegmentA has already been emitted.");
        }
    }
}
