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
using EndPoint = Amazon.XRay.Recorder.Core.Internal.Utils.EndPoint;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EndPointTests
    {
        [TestMethod]
        public void TestWithIP()
        {
            var ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);

            var ep = EndPoint.Of(ip);
            
            Assert.AreEqual(ip, ep.GetIPEndPoint());
        }
        
        [TestMethod]
        public void TestWithHostname()
        {
            var testHost = new HostEndPoint("localhost", 3001);
            var expectedIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3001);

            var ep = EndPoint.Of(testHost);
            
            Assert.AreEqual(expectedIP, ep.GetIPEndPoint());
        }
    }
}