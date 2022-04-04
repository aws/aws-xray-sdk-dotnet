//-----------------------------------------------------------------------------
// <copyright file="SegmentNamingStrategyTests.cs" company="Amazon.com">
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
using System.Net.Http;
using Amazon.XRay.Recorder.Core.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if !NETFRAMEWORK
using Microsoft.AspNetCore.Http;
#endif

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class SegmentNamingStrategyTests
    {
#if NETFRAMEWORK
        private HttpRequestMessage _request;
#else
        private HttpRequest _request;
#endif
        [TestInitialize]
        public void TestInitialize()
        {
#if NETFRAMEWORK
            _request = new HttpRequestMessage();
#else
            _request = new DefaultHttpContext().Request;
#endif
            _request.Headers.Add("Host", "hostName");
            Environment.SetEnvironmentVariable(SegmentNamingStrategy.EnvironmentVariableSegmentName, null);
        }

        [TestMethod]
        public void TestFixedNameWithoutEnvironmentVariable()
        {
            SegmentNamingStrategy strategy = new FixedSegmentNamingStrategy("fixedName");
            Assert.AreEqual("fixedName", strategy.GetSegmentName(_request));
        }

        [TestMethod]
        public void TestFixedNameWithEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable(SegmentNamingStrategy.EnvironmentVariableSegmentName, "EnvironmentName");
            SegmentNamingStrategy strategy = new FixedSegmentNamingStrategy("fixedName");
            Assert.AreEqual("EnvironmentName", strategy.GetSegmentName(_request));
        }

        [TestMethod]
        public void TestDynamicNameWithDefaultPattern()
        {
            var strategy = new DynamicSegmentNamingStrategy("fallbackName");
            Assert.AreEqual("hostName", strategy.GetSegmentName(_request));
        }

        [TestMethod]
        public void TestDynamicNameWithPatternNotMatch()
        {
            var strategy = new DynamicSegmentNamingStrategy("fallbackName", "fixedName");
            Assert.AreEqual("fallbackName", strategy.GetSegmentName(_request));
        }

        [TestMethod]
        public void TestDynamicNameWithEnvironmentOverride()
        {
            Environment.SetEnvironmentVariable(SegmentNamingStrategy.EnvironmentVariableSegmentName, "EnvironmentName");
            var strategy = new DynamicSegmentNamingStrategy("fallbackName", "fixedName");
            Assert.AreEqual("EnvironmentName", strategy.GetSegmentName(_request));
        }
    }
}
