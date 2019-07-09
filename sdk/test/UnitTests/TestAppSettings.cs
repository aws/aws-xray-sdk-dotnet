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

using System;
using System.Configuration;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestAppSettings
    {
        private const string DisableXRayTracingKey = "DisableXRayTracing";
        private const string UseRuntimeErrors = "UseRuntimeErrors";
        private const string CollectSqlQueries = "CollectSqlQueries";

        [TestCleanup]
        public void TestCleanup()
        {
            ConfigurationManager.AppSettings[DisableXRayTracingKey] = null;
            ConfigurationManager.AppSettings[UseRuntimeErrors] = null;
            AppSettings.Reset();
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, null);
            AWSXRayRecorder.Instance.Dispose();
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
            var recorder = GetRecorder();
            Assert.AreEqual(Core.Strategies.ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsSetToFalse()
        {
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "false";
            AppSettings.Reset();
            var recorder = GetRecorder();
            Assert.AreEqual(Core.Strategies.ContextMissingStrategy.LOG_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsInvalid()
        { 
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "invalid";
            AppSettings.Reset();
            var recorder = GetRecorder();
            Assert.AreEqual(Core.Strategies.ContextMissingStrategy.RUNTIME_ERROR ,recorder.ContextMissingStrategy);
        }


        [TestMethod]
        public void TestUseRuntimeErrorsNoKeyPresent()
        {
            AppSettings.Reset();
            var recorder = GetRecorder();
            Assert.AreEqual(Core.Strategies.ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestCollectSqlQueriesNoKeyPresent()
        {
            AppSettings.Reset();
            Assert.IsFalse(AppSettings.CollectSqlQueries);
        }

        [TestMethod]
        public void TestCollectSqlQueriesSetToFalse()
        {
            ConfigurationManager.AppSettings[CollectSqlQueries] = "false";
            AppSettings.Reset();

            Assert.IsFalse(AppSettings.CollectSqlQueries);
        }

        [TestMethod]
        public void TestCollectSqlQueriesSetToTrue()
        {
            ConfigurationManager.AppSettings[CollectSqlQueries] = "true";
            AppSettings.Reset();

            Assert.IsTrue(AppSettings.CollectSqlQueries);
        }

        [TestMethod]
        public void TestCollectSqlQueriesSetToInvalid()
        {
            ConfigurationManager.AppSettings[CollectSqlQueries] = "invalid";
            AppSettings.Reset();

            Assert.IsFalse(AppSettings.CollectSqlQueries);
        }

        private AWSXRayRecorder GetRecorder()
        {
            return new AWSXRayRecorderBuilder().WithPluginsFromAppSettings().WithContextMissingStrategyFromAppSettings().Build();
        }
    }
}
