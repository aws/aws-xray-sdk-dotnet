//-----------------------------------------------------------------------------
// <copyright file="LocalizedSamplingStrategyTests.cs" company="Amazon.com">
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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Sampling.Local;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class LocalizedSamplingStrategyTests
    {
        private const string ManifestKey = "SamplingRuleManifest";
#if !NET45
        private XRayOptions _xRayOtions = new XRayOptions();
#endif

        [TestCleanup]
        public void TestCleanup()
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = null;
            AppSettings.Reset();
#else
            _xRayOtions = new XRayOptions();
#endif
        }

        [TestMethod]
        public void TestFixedTargetAndRateBehaveCorrectly()
        {
            var strategy = new LocalizedSamplingStrategy();
            strategy.Rules.Add(new Core.Sampling.Local.SamplingRule("name", "test", "get", 10, 0.05));
            int count = 0;

            // fill the fixed target rate
            Action action = () =>
            {
                var input = new SamplingInput("name", "test", "get","", "");
                if (strategy.ShouldTrace(input).SampleDecision == SampleDecision.Sampled)
                {
                    Interlocked.Increment(ref count);
                }
            };
            var actions = Enumerable.Repeat(action, 10).ToArray();
            Parallel.Invoke(actions);
            Assert.AreEqual(10, count);

            count = 0;
            var samplingInput = new SamplingInput("name", "test", "get", "", "");
            for (int i = 0; i < 200; i++)
            {
                count += strategy.ShouldTrace(samplingInput).SampleDecision == SampleDecision.Sampled ? 1 : 0;
            }

            // Default sample rate is 5%. The chance that count == 0 after 200 tries is 0.003%.
            Assert.IsTrue(count > 0);
            Assert.IsTrue(count < 50);
        }

        [TestMethod]
        public void TestLocalFallbackRules() // If rules are not loaded by default sampler, use local rules
        {
            var strategy = new DefaultSamplingStrategy("JSONs\\SamplingRules1.json");

            var input = new SamplingInput("name", "test", "get", "", "");
            var samplingResponse = strategy.ShouldTrace(input);

            Assert.AreEqual(SampleDecision.Sampled, samplingResponse.SampleDecision);
            Assert.IsTrue(string.IsNullOrEmpty(samplingResponse.RuleName));
        }

        [TestMethod]
        public void TestDefaultRuleWithRequest()
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(@"http://www.amazon.com/api/product"),
                Method = HttpMethod.Post
            };
            request.Headers.Add("Host", "www.amazon.com");
            var strategy = new LocalizedSamplingStrategy();
            string host = request.Headers.Host;
            string url = request.RequestUri.AbsolutePath;
            string method = request.Method.Method;
            SamplingInput samplingInput = new SamplingInput(host, url, method, "", "");
            SamplingResponse samplingResponse = strategy.ShouldTrace(samplingInput);
            Assert.AreEqual(SampleDecision.Sampled, samplingResponse.SampleDecision);
            Assert.IsTrue(string.IsNullOrEmpty(samplingResponse.RuleName));
        }

        [TestMethod]
        public void TestLoadJsonConfiguration() // version 1
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = @"JSONs\DefaultSamplingRules.json";
            AppSettings.Reset();
            var strategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
#else
            _xRayOtions.SamplingRuleManifest = "JSONs\\DefaultSamplingRules.json";
            var strategy = new LocalizedSamplingStrategy(_xRayOtions.SamplingRuleManifest);
#endif
            Assert.AreEqual(2, strategy.Rules.Count);

            Assert.AreEqual(@"checkout", strategy.Rules[0].Host);
            Assert.AreEqual(@"POST", strategy.Rules[0].HttpMethod);
            Assert.AreEqual(@"/checkout", strategy.Rules[0].UrlPath);
            Assert.AreEqual(10, strategy.Rules[0].FixedTarget);
            Assert.AreEqual(@"0.3", strategy.Rules[0].Rate.ToString("0.0"));
            Assert.AreEqual("This is the first rule.", strategy.Rules[0].Description);

            Assert.AreEqual(@"base.com", strategy.Rules[1].Host);
            Assert.AreEqual(@"GET", strategy.Rules[1].HttpMethod);
            Assert.AreEqual(@"*", strategy.Rules[1].UrlPath);
            Assert.AreEqual(100, strategy.Rules[1].FixedTarget);
            Assert.AreEqual(@"0.5", strategy.Rules[1].Rate.ToString("0.0"));
            Assert.AreEqual("Rule for base.com", strategy.Rules[1].Description);
        }

        [TestMethod]
        public void TestLoadJsonConfiguration1() // Version 2
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = @"JSONs\DefaultSamplingRules1.json";
            AppSettings.Reset();
            var strategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
#else
            _xRayOtions.SamplingRuleManifest = "JSONs\\DefaultSamplingRules.json";
            var strategy = new LocalizedSamplingStrategy(_xRayOtions.SamplingRuleManifest);
#endif
            Assert.AreEqual(2, strategy.Rules.Count);

            Assert.AreEqual(@"checkout", strategy.Rules[0].Host);
            Assert.AreEqual(@"POST", strategy.Rules[0].HttpMethod);
            Assert.AreEqual(@"/checkout", strategy.Rules[0].UrlPath);
            Assert.AreEqual(10, strategy.Rules[0].FixedTarget);
            Assert.AreEqual(@"0.3", strategy.Rules[0].Rate.ToString("0.0"));
            Assert.AreEqual("This is the first rule.", strategy.Rules[0].Description);

            Assert.AreEqual(@"base.com", strategy.Rules[1].Host);
            Assert.AreEqual(@"GET", strategy.Rules[1].HttpMethod);
            Assert.AreEqual(@"*", strategy.Rules[1].UrlPath);
            Assert.AreEqual(100, strategy.Rules[1].FixedTarget);
            Assert.AreEqual(@"0.5", strategy.Rules[1].Rate.ToString("0.0"));
            Assert.AreEqual("Rule for base.com", strategy.Rules[1].Description);
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidSamplingConfigurationException))]
        public void TestLoadSamplingRulesWithoutDefaultRule()
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = @"JSONs\SamplingRulesWithoutDefault.json";
            AppSettings.Reset();
            var strategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
#else
            _xRayOtions.SamplingRuleManifest = @"JSONs\SamplingRulesWithoutDefault.json";
            var strategy = new LocalizedSamplingStrategy(_xRayOtions.SamplingRuleManifest);
#endif
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidSamplingConfigurationException))]
        public void TestLoadSamplingRulesWithInvalidVersion1()
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = @"JSONs\SamplingRulesInvalidV1.json";
            AppSettings.Reset();
            var strategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
#else
            _xRayOtions.SamplingRuleManifest = @"JSONs\SamplingRulesWithoutDefault.json";
            var strategy = new LocalizedSamplingStrategy(_xRayOtions.SamplingRuleManifest);
#endif
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidSamplingConfigurationException))]
        public void TestLoadSamplingRulesWithInvalidVersion2()
        {
#if NET45
            ConfigurationManager.AppSettings[ManifestKey] = @"JSONs\SamplingRulesInvalidV2.json";
            AppSettings.Reset();
            var strategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
#else
            _xRayOtions.SamplingRuleManifest = @"JSONs\SamplingRulesWithoutDefault.json";
            var strategy = new LocalizedSamplingStrategy(_xRayOtions.SamplingRuleManifest);
#endif
        }

        [TestMethod]
        public void TestDefaultSamplingRuleWhenNoConfigurationSpecified()
        {
            var strategy = new LocalizedSamplingStrategy();
            Assert.AreEqual(0, strategy.Rules.Count);
            Core.Sampling.Local.SamplingRule rule = strategy.DefaultRule;
            Assert.AreEqual("*", rule.Host);
            Assert.AreEqual("*", rule.UrlPath);
            Assert.AreEqual("*", rule.HttpMethod);
            Assert.AreEqual(1, rule.FixedTarget);
            Assert.AreEqual("0.05", rule.Rate.ToString("0.00"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidSamplingConfigurationException))]
        public void TestLoadingInvalidSamplingRules()
        {
            new LocalizedSamplingStrategy(@"JSONs\InvalidSamplingRules.json");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidSamplingConfigurationException))]
        public void TestUnsupportedSamplingVersion()
        {
            new LocalizedSamplingStrategy(@"JSONs\UnsupportedSamplingConfigurationVersion.json");
        }
    }
}
