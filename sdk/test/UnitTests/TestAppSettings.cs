//-----------------------------------------------------------------------------
// <copyright file="TestAppSettings.cs" company="Amazon.com">
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

using System.Configuration;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestAppSettings
    {
        private const string DisableXRayTracingKey = "DisableXRayTracing";
        private const string UseRuntimeErrors = "UseRuntimeErrors";

        [TestCleanup]
        public void TestCleanup()
        {
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = null;
            AppSettings.Reset();
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyTrue()
        {
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = "true";
            AppSettings.Reset();
            Assert.IsTrue(AppSettings.IsXRayTracingDisabled);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyFalse()
        {
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = "false";
            AppSettings.Reset();
            Assert.IsFalse(AppSettings.IsXRayTracingDisabled);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyMissing()
        {
            AppSettings.Reset();
            Assert.IsFalse(AppSettings.IsXRayTracingDisabled);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyInvalid()
        {
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = "invalid";
            AppSettings.Reset();
            Assert.IsFalse(AppSettings.IsXRayTracingDisabled);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsSetToTrue()
        {
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "true";
            AppSettings.Reset();
            Assert.IsTrue(AppSettings.UseRuntimeErrors);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsSetToFalse()
        {
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "false";
            AppSettings.Reset();
            Assert.IsFalse(AppSettings.UseRuntimeErrors);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsInvalid()
        {
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "XYZ";
            AppSettings.Reset();
            Assert.IsTrue(AppSettings.UseRuntimeErrors);
        }
    }
}
