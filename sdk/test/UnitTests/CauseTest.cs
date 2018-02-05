//-----------------------------------------------------------------------------
// <copyright file="CauseTest.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class CauseTest : TestBase
    {
        [TestMethod]
        public void TestMultipleInnerException()
        {
            using (var recorder = AWSXRayRecorder.Instance)
            {
                recorder.BeginSegment("parent", TraceId);
                var segment = TraceContext.GetEntity();

                var innerException = new ArgumentNullException("value");
                var exception = new EntityNotAvailableException("text", innerException);

                recorder.BeginSubsegment("child");
                recorder.AddException(exception);
                recorder.EndSubsegment();

                recorder.EndSegment();

                Assert.IsNull(segment.Cause);
                Assert.AreEqual(2, segment.Subsegments[0].Cause.ExceptionDescriptors.Count);
                Assert.AreEqual(segment.Subsegments[0].Cause.ExceptionDescriptors[0].Cause, segment.Subsegments[0].Cause.ExceptionDescriptors[1].Id);
                Assert.AreEqual("EntityNotAvailableException", segment.Subsegments[0].Cause.ExceptionDescriptors[0].Type);
                Assert.AreEqual("ArgumentNullException", segment.Subsegments[0].Cause.ExceptionDescriptors[1].Type);
            }
        }

        [TestMethod]
        public void TestInnerExceptionReference()
        {
            using (var recorder = AWSXRayRecorder.Instance)
            {
                recorder.BeginSegment("parent", TraceId);
                var segment = TraceContext.GetEntity();

                try
                {
                    recorder.BeginSubsegment("child");
                    try
                    {
                        throw new ArgumentNullException("value");
                    }
                    catch (ArgumentNullException e)
                    {
                        recorder.AddException(e);
                        recorder.EndSubsegment();
                        throw new EntityNotAvailableException("Dummy message", e);
                    }
                }
                catch (EntityNotAvailableException e)
                {
                    recorder.AddException(e);
                    recorder.EndSegment();
                }

                Assert.AreEqual("Dummy message", segment.Cause.ExceptionDescriptors[0].Message);
                Assert.AreEqual("EntityNotAvailableException", segment.Cause.ExceptionDescriptors[0].Type);
                Assert.AreEqual(segment.Cause.ExceptionDescriptors[0].Cause, segment.Subsegments[0].Cause.ExceptionDescriptors[0].Id);
                Assert.IsNull(segment.Cause.ReferenceExceptionId);
                Assert.AreEqual(1, segment.Cause.ExceptionDescriptors.Count);

                Assert.AreEqual("ArgumentNullException", segment.Subsegments[0].Cause.ExceptionDescriptors[0].Type);
            }
        }

        [TestMethod]
        public void TestReferenceExceptionFromOtherSubsegment()
        {
            using (var recorder = AWSXRayRecorder.Instance)
            {
                recorder.BeginSegment("parent", TraceId);
                var segment = TraceContext.GetEntity();

                try
                {
                    recorder.BeginSubsegment("child");
                    try
                    {
                        throw new ArgumentNullException("value");
                    }
                    catch (ArgumentNullException e)
                    {
                        recorder.AddException(e);
                        recorder.EndSubsegment();
                        throw e;
                    }
                }
                catch (ArgumentNullException e)
                {
                    recorder.AddException(e);
                    recorder.EndSegment();
                }

                Assert.AreEqual(segment.Cause.ReferenceExceptionId, segment.Subsegments[0].Cause.ExceptionDescriptors[0].Id);
                Assert.IsNull(segment.Cause.ExceptionDescriptors);
            }
        }
    }
}
