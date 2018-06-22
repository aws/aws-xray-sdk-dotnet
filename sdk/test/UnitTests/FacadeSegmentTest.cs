//-----------------------------------------------------------------------------
// <copyright file="FacadeSegmentTest.cs" company="Amazon.com">
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

using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class FacadeSegmentTest : TestBase
    {
        private FacadeSegment _facadeSegment;
        private const String ParentId = "53995c3f42cd8ad8";

        [TestInitialize]
        public void Initialize()
        {
            _facadeSegment = new FacadeSegment("Facade", TraceId, ParentId);
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
        }

        [TestMethod]
        public void TestFacadeSegmentId()
        {
            Assert.AreEqual(ParentId, _facadeSegment.Id);
        }

        [TestMethod]
        public void TestFacadeSegmentOrigin()
        {
            try
            {
                _facadeSegment.Origin = "test";
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentService()
        {
            var t = _facadeSegment.Service;
            Assert.IsNull(t);
        }

        [TestMethod]
        public void TestFacadeSegmentIsServiceAdded()
        {
            var t = _facadeSegment.IsServiceAdded;
            Assert.IsFalse(t);
        }

        [TestMethod]
        public void TestFacadeSegmentSetStartTime()
        {
            try
            {
                _facadeSegment.SetStartTime(DateTime.Now);
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentSetStartTimeToNow()
        {
            try
            {
                _facadeSegment.SetStartTimeToNow();
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentSetEndTime()
        {
            try
            {
                _facadeSegment.SetEndTime(DateTime.Now);
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentSetEndTimeToNow()
        {
            try
            {
                _facadeSegment.SetEndTimeToNow();
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentAddMetaData()
        {
            try
            {
                _facadeSegment.AddMetadata("", null);
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeAddExceptionn()
        {
            try
            {
                _facadeSegment.AddException(null);
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeAddAnnotation()
        {
            try
            {
                _facadeSegment.AddAnnotation("", null);
                Assert.Fail();
            }
            catch (UnsupportedOperationException)
            {
                //expected
            }
        }

        [TestMethod]
        public void TestFacadeSegmentHttp()
        {
            var t = _facadeSegment.Http;
            Assert.IsNull(t);
        }

        [TestMethod]
        public void TestFacadeSegmentSql()
        {
            var t = _facadeSegment.Sql;
            Assert.IsNull(t);
        }

        [TestMethod]
        public void TestFacadeSegmentIsThrottled()
        {
            var t = _facadeSegment.IsThrottled;
            Assert.IsFalse(t);
        }

        [TestMethod]
        public void TestFacadeSegmentHasError()
        {
            var t = _facadeSegment.HasError;
            Assert.IsFalse(t);
        }

        [TestMethod]
        public void TestFacadeSegmentHasFault()
        {
            var t = _facadeSegment.HasFault;
            Assert.IsFalse(t);
        }

        [TestMethod]
        public void TestFacadeSegmentHasIsHttpAdded()
        {
            var t = _facadeSegment.IsHttpAdded;
            Assert.IsFalse(t);
        }
    }
}
