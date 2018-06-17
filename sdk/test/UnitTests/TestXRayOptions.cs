﻿//-----------------------------------------------------------------------------
// <copyright file="TestXRayOptions.cs" company="Amazon.com">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.Extensions.Configuration;
using System.IO;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Sampling;
using System.Net.Http;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestXRayOptions
    {
        private const string DisableXRayTracingKey = "DisableXRayTracing";

        private static XRayOptions _xRayOptions;

        private static string PREFIX = @"JSONs\Appsetting\";

        [TestCleanup]
        public void TestCleanup()
        {
            _xRayOptions = new XRayOptions();
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyTrue()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayTrue.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsTrue(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyFalse()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayFalse.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyMissing()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayMissing.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestDisableXRayTracingKeyInvalid()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayInvalid.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestConfigurationIsNull()
        {
            IConfiguration configuration = BuildConfiguration("NoXRaySection.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestXRaySectionMissing() // No "Xray" section in the json
        {
            IConfiguration configuration = null;

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
        }

        [TestMethod]
        public void TestInitializeInstance()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayTrue.json");

            AWSXRayRecorder.InitializeInstance(configuration);
            _xRayOptions = AWSXRayRecorder.Instance.XRayOptions;

            Assert.IsTrue(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
            Assert.IsTrue(_xRayOptions.UseRuntimeErrors);

            Assert.AreEqual(typeof(UdpSegmentEmitter), AWSXRayRecorder.Instance.Emitter.GetType()); // Default emitter set

            AWSXRayRecorder.Instance.Dispose();
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorder1()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayTrue.json");

            AWSXRayRecorder recorder = BuildAWSXRayRecorder(new TestSamplingStrategy());
 
            AWSXRayRecorder.InitializeInstance(configuration, recorder);
            _xRayOptions = recorder.XRayOptions;
            Assert.IsTrue(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
            Assert.IsTrue(_xRayOptions.UseRuntimeErrors);

            Assert.AreEqual(AWSXRayRecorder.Instance.SamplingStrategy, recorder.SamplingStrategy); // Custom recorder set in TraceContext
            Assert.AreEqual(typeof(UdpSegmentEmitter), recorder.Emitter.GetType()); // Default emitter set
            recorder.Dispose();
        }

        [TestMethod]
        public void TestInitializeInstanceWithRecorder2()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayTrue.json");

            AWSXRayRecorder recorder = BuildAWSXRayRecorder(new TestSamplingStrategy(), new DummyEmitter());

            AWSXRayRecorder.InitializeInstance(configuration, recorder);
            _xRayOptions = recorder.XRayOptions;
            Assert.IsTrue(_xRayOptions.IsXRayTracingDisabled);
            Assert.IsNull(_xRayOptions.AwsServiceHandlerManifest);
            Assert.IsNull(_xRayOptions.PluginSetting);
            Assert.IsNull(_xRayOptions.SamplingRuleManifest);
            Assert.IsTrue(_xRayOptions.UseRuntimeErrors);

            Assert.AreEqual(AWSXRayRecorder.Instance.SamplingStrategy, recorder.SamplingStrategy); // Custom recorder set in TraceContext
            Assert.AreEqual(typeof(DummyEmitter), recorder.Emitter.GetType()); // custom emitter set
            recorder.Dispose();
        }
        
        [TestMethod]
        public void TestUseRuntimeErrorsFalse()
        {
            IConfiguration configuration = BuildConfiguration("UseRuntimeErrorsFalse.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsFalse(_xRayOptions.UseRuntimeErrors);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsTrue()
        {
            IConfiguration configuration = BuildConfiguration("UseRuntimeErrorsTrue.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsTrue(_xRayOptions.UseRuntimeErrors);
        }

        [TestMethod]
        public void TestUseRuntimeErrorsDefaultsTrue_WhenNotSpecifiedInJson()
        {
            IConfiguration configuration = BuildConfiguration("DisabledXRayMissing.json");

            _xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            Assert.IsTrue(_xRayOptions.UseRuntimeErrors);
        }

        // Creating custom configuration
        private IConfiguration BuildConfiguration(string path)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                              .SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile(PREFIX + path);
            IConfiguration configuration = builder.Build();
            return configuration;
        }
        private static AWSXRayRecorder BuildAWSXRayRecorder(ISamplingStrategy samplingStrategy, ISegmentEmitter segmentEmitter=null)
        {
            AWSXRayRecorderBuilder builder;
            if (segmentEmitter != null){
                 builder = new AWSXRayRecorderBuilder()
                        .WithSamplingStrategy(samplingStrategy).WithSegmentEmitter(segmentEmitter);
            }
            else
            {
                 builder = new AWSXRayRecorderBuilder()
                        .WithSamplingStrategy(samplingStrategy);
            }

            var result = builder.Build();

            return result;
        }
    }
    class TestSamplingStrategy : ISamplingStrategy
    {
        public SampleDecision Sample(string serviceName, string path, string method)
        {
            throw new System.NotImplementedException();
        }

        public SampleDecision Sample(HttpRequestMessage request)
        {
            throw new System.NotImplementedException();
        }
    }

    class DummyEmitter : ISegmentEmitter
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
