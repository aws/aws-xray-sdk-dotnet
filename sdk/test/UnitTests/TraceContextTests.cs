//-----------------------------------------------------------------------------
// <copyright file="TraceContextTests.cs" company="Amazon.com">
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

using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TraceContextTests : TestBase
    {
        [TestMethod]
        public void TestSyncGet()
        {
            var segment = new Segment("test", TraceId);

            AWSXRayRecorder.Instance.SetEntity(segment);
            var later = AWSXRayRecorder.Instance.GetEntity();

            Assert.ReferenceEquals(segment, later);
        }

        [TestMethod]
        public async Task TestAsyncGet()
        {
            var origin = new Subsegment("test");

            AWSXRayRecorder.Instance.SetEntity(origin);

            Entity later = null;

            await Task.Run(() =>
            {
                later = AWSXRayRecorder.Instance.GetEntity();
            });

            Assert.ReferenceEquals(origin, later);
        }

        [TestMethod]
        public async Task TestAsyncGetAfterOverwrite()
        {
            var segment1 = new Segment("hello", TraceId);
            var segment2 = new Subsegment("bye");

            AWSXRayRecorder.Instance.SetEntity(segment1);

            Entity later = null;

            Task task = Task.Run(() =>
            {
                Thread.Sleep(10);    // Wait for overwrite to complete
                later = AWSXRayRecorder.Instance.GetEntity();
            });

            AWSXRayRecorder.Instance.SetEntity(segment2);  // Overwrite in parent
            await task;
            Assert.ReferenceEquals(later, segment1);
        }

        [TestMethod]
        public void TestSegmentExistAfterSet()
        {
            AWSXRayRecorder.Instance.SetEntity(new Segment("test", TraceId));
            Assert.IsNotNull(AWSXRayRecorder.Instance.GetEntity());
        }
    }
}
