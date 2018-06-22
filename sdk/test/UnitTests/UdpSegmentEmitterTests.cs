//-----------------------------------------------------------------------------
// <copyright file="UdpSegmentEmitterTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class UdpSegmentEmitterTests
    {
        [TestMethod]
        public void TestDefaultDaemonAddress()
        {
            var emitter = new UdpSegmentEmitter();
            var expected = "127.0.0.1:2000";
            var actual = emitter.EndPoint.ToString();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestSetDaemonAddress()
        {
            var emitter = new UdpSegmentEmitter();
            string newAddress = "10.10.10.10:1234";
            emitter.SetDaemonAddress(newAddress);
            Assert.AreEqual(newAddress, emitter.EndPoint.ToString());
        }

        [TestMethod]
        public void TestEnvironmentVariableOverrideSetDaemonAddress()
        {
            var environmentAddress = "1.1.1.1:123";
            var localAddress = "2.2.2.2:456";

            Environment.SetEnvironmentVariable(DaemonConfig.EnvironmentVariableDaemonAddress, environmentAddress);
            var emitter = new UdpSegmentEmitter();
            emitter.SetDaemonAddress(localAddress);

            Assert.AreEqual(environmentAddress, emitter.EndPoint.ToString());
            Environment.SetEnvironmentVariable(DaemonConfig.EnvironmentVariableDaemonAddress, null);
        }
    }
}
