//-----------------------------------------------------------------------------
// <copyright file="DaemonConfigTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class DaemonConfigTests
    {
        [TestMethod]
        public void TestGetDefualtEndpoint()
        {
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint();
            Assert.IsTrue(daemonConfig.UDPEndpoint.Equals(DaemonConfig.DefaultEndpoint));
            Assert.IsTrue(daemonConfig.TCPEndpoint.Equals(DaemonConfig.DefaultEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointSingleForm()
        {
            string ip = "1.0.0.1";
            int port = 1000;
            string daemonAddress = ip + ":" + port;
            IPEndPoint expectedEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(expectedEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(expectedEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointDoubleForm1()
        {
            string daemonAddress = "tcp:127.0.0.1:2000 udp:127.0.0.2:2001";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(expectedUDPEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(expectedTCPEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointDoubleForm2()
        {
            string daemonAddress = "udp:127.0.0.2:2001 tcp:127.0.0.1:2000";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(expectedUDPEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(expectedTCPEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointInvalidDoubleForm1()
        {
            string daemonAddress = "tcp:127.0.0.2:2001 tcp:127.0.0.1:2000";
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointInvalidDoubleForm2()
        {
            string daemonAddress = "udp:127.0.0.2:2001 udp:127.0.0.1:2000";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointInvalidDoubleForm3()
        {
            string daemonAddress = "lmn:127.0.0.2:2001 udp:127.0.0.1:2000";
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointInvalidSingleForm()
        {
            string daemonAddress = "1000001.1.1.1:9000"; // invalid ip address
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.UDPEndpoint));
            Assert.IsTrue(DaemonConfig.DefaultEndpoint.Equals(daemonConfig.TCPEndpoint));
        }

        [TestMethod]
        public void TestGetEndpointEnvironemntVariableGettingPreference()
        {
            string daemonAddress = "1.0.0.2:2001";
            // Setting Enviornment variable 
            string envName = DaemonConfig.EnvironmentVariableDaemonAddress;
            string value = "udp:127.0.0.2:2001 tcp:127.0.0.1:2000";
            IPEndPoint expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.2"), 2001);
            IPEndPoint expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);

            Environment.SetEnvironmentVariable(envName, value);
            DaemonConfig daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.IsTrue(expectedUDPEndpoint.Equals(daemonConfig.UDPEndpoint)); // Environment value used 
            Assert.IsTrue(expectedTCPEndpoint.Equals(daemonConfig.TCPEndpoint));

            Environment.SetEnvironmentVariable(envName, null); // cleaning the environment variable
        }

        [TestMethod]
        public void TestGetEndpointSingleFormWithHostname()
        {
            var daemonAddress = "localhost:3001";
            var expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);
            var expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);
            var daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.AreEqual(daemonConfig.UDPEndpoint, expectedUDPEndpoint);
            Assert.AreEqual(daemonConfig.TCPEndpoint, expectedTCPEndpoint);
        }
        
        [TestMethod]
        public void TestGetEndpointDoubleFormWithHostname()
        {
            var tcpPort = 3001;
            var udpPort = 2001;
            var daemonAddress = $"tcp:localhost:{tcpPort} udp:localhost:{udpPort}";
            var expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), tcpPort);
            var expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), udpPort);
            var daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.AreEqual(daemonConfig.UDPEndpoint, expectedUDPEndpoint);
            Assert.AreEqual(daemonConfig.TCPEndpoint, expectedTCPEndpoint);
        }
        
        [TestMethod]
        public void TestGetEndpointDoubleFormWithHostnameIpMix1()
        {
            var tcpPort = 3001;
            var udpPort = 2001;
            var daemonAddress = $"tcp:127.0.0.1:{tcpPort} udp:localhost:{udpPort}";
            var expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), tcpPort);
            var expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), udpPort);
            var daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.AreEqual(daemonConfig.UDPEndpoint, expectedUDPEndpoint);
            Assert.AreEqual(daemonConfig.TCPEndpoint, expectedTCPEndpoint);
        }
        
        [TestMethod]
        public void TestGetEndpointDoubleFormWithHostnameIpMix2()
        {
            var tcpPort = 3001;
            var udpPort = 2001;
            var daemonAddress = $"tcp:localhost:{tcpPort} udp:127.0.0.1:{udpPort}";
            var expectedTCPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), tcpPort);
            var expectedUDPEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), udpPort);
            var daemonConfig = DaemonConfig.GetEndPoint(daemonAddress);
            Assert.AreEqual(daemonConfig.UDPEndpoint, expectedUDPEndpoint);
            Assert.AreEqual(daemonConfig.TCPEndpoint, expectedTCPEndpoint);
        }

    }
}
