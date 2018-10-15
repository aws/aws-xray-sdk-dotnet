//-----------------------------------------------------------------------------
// <copyright file="AwsXrayRecorderBuilderTests.cs" company="Amazon.com">
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

using System.Collections.Generic;
using System.Configuration;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Plugins;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using System;
using Amazon.XRay.Recorder.Core.Internal.Context;


#if !NET45
using Microsoft.Extensions.Configuration;
#endif

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class AwsXrayRecorderBuilderTests : TestBase
    {
        private const string PluginKey = "AWSXRayPlugins";
        private const string UseRuntimeErrors = "UseRuntimeErrors";
#if !NET45
        private XRayOptions _xRayOptions = new XRayOptions();
#endif

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
#if NET45
            ConfigurationManager.AppSettings[PluginKey] = null;
            ConfigurationManager.AppSettings[UseRuntimeErrors] = null;
            AppSettings.Reset();
#else
            _xRayOptions = new XRayOptions();
#endif
        }

        [TestMethod]
        public void TestBuildWithDummyPlugin()
        {
            var dummyPlugin = new DummyPlugin();
            AWSXRayRecorder recorder = new AWSXRayRecorderBuilder().WithPlugin(dummyPlugin).Build();

            recorder.BeginSegment("test", TraceId);
            var segment = (Segment)AWSXRayRecorder.Instance.TraceContext.GetEntity();
            recorder.EndSegment();

            Assert.AreEqual("Origin", segment.Origin);

            var dict = segment.Aws[dummyPlugin.ServiceName] as Dictionary<string, object>;
            Assert.AreEqual("value1", dict["key1"]);
        }

        [TestMethod]
        public void TestWithDefaultPlugins()
        {

#if NET45
            ConfigurationManager.AppSettings[PluginKey] = "EC2Plugin";
            AppSettings.Reset();
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithPluginsFromAppSettings();
#else
            _xRayOptions.PluginSetting = "EC2Plugin";
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithPluginsFromConfig(_xRayOptions);
#endif

            Assert.AreEqual(1, builder.Plugins.Count);
            var expectedType = typeof(EC2Plugin);
            var actualType = builder.Plugins[0].GetType();
            Assert.AreEqual(expectedType, actualType);
        }

        [TestMethod]
        public void TestPluginSettingMissing()
        {
#if NET45
            var builder = new AWSXRayRecorderBuilder().WithPluginsFromAppSettings();
#else
            var builder = new AWSXRayRecorderBuilder().WithPluginsFromConfig(_xRayOptions);
#endif
            Assert.AreEqual(0, builder.Plugins.Count);
        }

        [TestMethod]
        public void TestInvalidPluginSetting()
        {
#if NET45
            ConfigurationManager.AppSettings[PluginKey] = "UnknownPlugin, IPlugin";
            AppSettings.Reset();
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithPluginsFromAppSettings();
#else
            _xRayOptions.PluginSetting = "UnknownPlugin, IPlugin";
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithPluginsFromConfig(_xRayOptions);
#endif
            Assert.AreEqual(0, builder.Plugins.Count);
        }

        [TestMethod]
        public void TestSetSamplingStrategy()
        {
            var recorder = new AWSXRayRecorderBuilder().WithSamplingStrategy(new DummySamplingStrategy()).Build();
            Assert.AreEqual(typeof(DummySamplingStrategy).FullName, recorder.SamplingStrategy.GetType().FullName);
        }

        [TestMethod]
        public void TestDefaultValueOfContextMissingStrategy()
        {
            var recorder = new AWSXRayRecorderBuilder().Build();
            Assert.AreEqual(ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestSetContextMissingStrategy()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).Build();
            Assert.AreEqual(ContextMissingStrategy.LOG_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestSetContextMissingUsingConfiguration1() // Contextmissing startegy set to log error from configuration
        {
#if NET45
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "false";
            AppSettings.Reset();
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromAppSettings();
#else
            _xRayOptions.UseRuntimeErrors = false;
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromConfig(_xRayOptions);
#endif
            AWSXRayRecorder recorder = builder.Build();
            Assert.AreEqual(ContextMissingStrategy.LOG_ERROR, recorder.ContextMissingStrategy);
        }

        [TestMethod]
        public void TestSetContextMissingUsingConfiguration2() // Contextmissing startegy not set
        {
#if NET45
            AppSettings.Reset();
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromAppSettings();
#else
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromConfig(_xRayOptions);
#endif
            AWSXRayRecorder recorder = builder.Build();
            Assert.AreEqual(ContextMissingStrategy.RUNTIME_ERROR, recorder.ContextMissingStrategy); // Default context missing strategy is set
        }

        [TestMethod]
        public void TestSetContextMissingUsingConfiguration3() // Contextmissing startegy is set through environment and configurations
        {
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, "LOG_ERROR");
#if NET45
            ConfigurationManager.AppSettings[UseRuntimeErrors] = "true";
            AppSettings.Reset();
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromAppSettings();
#else
            _xRayOptions.UseRuntimeErrors = true;
            AWSXRayRecorderBuilder builder = new AWSXRayRecorderBuilder().WithContextMissingStrategyFromConfig(_xRayOptions);
#endif
            AWSXRayRecorder recorder = builder.Build();
            Assert.AreEqual(ContextMissingStrategy.LOG_ERROR, recorder.ContextMissingStrategy); // Preference given to environment variable
            Environment.SetEnvironmentVariable(AWSXRayRecorder.EnvironmentVariableContextMissingStrategy, null);
        }

        [TestMethod]
        public void TestSetEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).WithSegmentEmitter(new DummyEmitter()).Build();
            Assert.AreEqual(typeof(DummyEmitter).FullName, recorder.Emitter.GetType().FullName);
        }

        [TestMethod]
        public void TestSetDefaultEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).Build(); // set default UDP emitter
            Assert.AreEqual(typeof(UdpSegmentEmitter).FullName, recorder.Emitter.GetType().FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetNullEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).WithSegmentEmitter(null).Build();
            Assert.Fail();
        }

        [TestMethod]
        public void TestSetTraceContext()
        {
            var recorder = new AWSXRayRecorderBuilder().WithTraceContext(new DummyTraceContext()).Build(); // set custom trace context
            Assert.AreEqual(typeof(DummyTraceContext).FullName, recorder.TraceContext.GetType().FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetNullTraceContext()
        {
            var recorder = new AWSXRayRecorderBuilder().WithTraceContext(null).Build();
            Assert.Fail();
        }

        private class DummySamplingStrategy : ISamplingStrategy
        {
            public SamplingResponse ShouldTrace(SamplingInput input)
            {
                throw new NotImplementedException();
            }
        }

        private class DummyPlugin : IPlugin
        {
            public string Origin
            {
                get
                {
                    return "Origin";
                }
            }

            public string ServiceName
            {
                get
                {
                    return "DummyService";
                }
            }

            public bool TryGetRuntimeContext(out IDictionary<string, object> context)
            {
                context = new Dictionary<string, object>();
                context.Add("key1", "value1");
                return true;
            }
        }

        private class DummyEmitter : ISegmentEmitter
        {
            public void Dispose()
            {
            }

            public void Send(Entity segment)
            {
            }

            public void SetDaemonAddress(string daemonAddress)
            {
            }
        }

       public class DummyTraceContext : ITraceContext
        {
            public void ClearEntity()
            {
            }

            public Entity GetEntity()
            {
                throw new NotImplementedException();
            }

            public bool IsEntityPresent()
            {
                throw new NotImplementedException();
            }

            public void SetEntity(Entity entity)
            {
                throw new NotImplementedException();
            }
        }
    }
}
