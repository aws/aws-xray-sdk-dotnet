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


#if !NET45
using Microsoft.Extensions.Configuration;
#endif

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class AwsXrayRecorderBuilderTests : TestBase
    {
        private const string PluginKey = "AWSXRayPlugins";
#if !NET45
        private XRayOptions _xRayOptions = new XRayOptions();
#endif

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
#if NET45
            ConfigurationManager.AppSettings[PluginKey] = null;
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
            var segment = (Segment)TraceContext.GetEntity();
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
        public void TestSetEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).WithSegmentEmitter(new DummyEmitter()).Build();
            Assert.AreEqual(typeof(DummyEmitter).FullName, recorder.Emitter.GetType().FullName);
        }

        [TestMethod]
        public void TestSetDefaultEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).Build(); // set defualt UDP emitter
            Assert.AreEqual(typeof(UdpSegmentEmitter).FullName, recorder.Emitter.GetType().FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetNullEmitter()
        {
            var recorder = new AWSXRayRecorderBuilder().WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR).WithSegmentEmitter(null).Build();
            Assert.Fail();
        }

        private class DummySamplingStrategy : ISamplingStrategy
        {
            public SampleDecision Sample(string serviceName, string path, string method)
            {
                throw new System.NotImplementedException();
            }

            public SampleDecision Sample(System.Net.Http.HttpRequestMessage request)
            {
                throw new System.NotImplementedException();
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
    }
}
