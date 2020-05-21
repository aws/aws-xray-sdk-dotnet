//-----------------------------------------------------------------------------
// <copyright file="IPEndPointExceptionTests.cs" company="Amazon.com">
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

using System.Net;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class IPEndPointExceptionTests
    {
        private readonly string[] _validIPs =
        {
            "127.0.0.1:0",
            "255.255.255.255:65535",
            "30.112.90.186:13",
            "244.161.162.143:25",
            "198.64.120.43:78",
            "117.241.174.52:158",
            "215.227.117.176:495",
            "86.75.47.108:394",
            "48.127.252.130:907",
            "152.167.110.181:1374",
            "24.199.113.13:8392",
            "105.20.156.48:1839",
            "83.21.91.200:8472",
            "8.11.252.222:19374",
            "237.114.144.69:9198",
            "189.190.176.237:12984",
            "62.174.113.213:54713",
            "86.222.140.110:18734",
            "167.252.123.17:9373",
            "17.24.65.100:382",
            "45.156.46.52:935",
            "166.43.20.149:9471"
        };

        private readonly string[] _invalidIPs =
        {
            "127.0.0.1",
            ".0.0.1:123",
            "999.999.999.999:1234",
            "10.56.12.34:70000",
            "300.0.0.1:1",
            "127.12.1234.1:22"
        };

        private readonly string[] _validDomains =
        {
            "example.com:0",
            "example.com:65535",
            "www.amazon.com:2000"
        };

        private readonly string[] _invalidDomains =
        {
            "example.com",
            "example.com:70000",
            "example.com:-100"
        };

        [TestMethod]
        public void TestValidIpAddresses()
        {
            IPEndPoint endpoint;
            foreach (var ip in _validIPs)
            {
                Assert.IsTrue(IPEndPointExtension.TryParse(ip, out endpoint));
            }
        }

        [TestMethod]
        public void TestValidDomains()
        {
            HostEndPoint endpoint;
            foreach (var domain in _validDomains)
            {
                Assert.IsTrue(IPEndPointExtension.TryParse(domain, out endpoint));
            }
        }

        [TestMethod]
        public void TestInvalidDomains()
        {
            HostEndPoint endpoint;
            foreach (var domain in _invalidDomains)
            {
                Assert.IsFalse(IPEndPointExtension.TryParse(domain, out endpoint));
            }
        }

        [TestMethod]
        public void TestInvalidIpAddress()
        {
            IPEndPoint endPoint;
            foreach (var ip in _invalidIPs)
            {
                Assert.IsFalse(IPEndPointExtension.TryParse(ip, out endPoint));
            }
        }
    }
}
