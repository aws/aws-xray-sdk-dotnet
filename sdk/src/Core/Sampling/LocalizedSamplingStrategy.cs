//-----------------------------------------------------------------------------
// <copyright file="LocalizedSamplingStrategy.cs" company="Amazon.com">
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// This strategy loads the sample Rules from local JSON file, and make the sample decision locally
    /// according to the Rules.
    /// </summary>
    public class LocalizedSamplingStrategy : ISamplingStrategy
    {
        private const string DefaultSamplingConfigurationResourceName = "Amazon.XRay.Recorder.Core.Sampling.DefaultSamplingRule.json";
        private const int SupportedSamplingConfigurationVersion = 1;
        private static readonly Logger _logger = Logger.GetLogger(typeof(LocalizedSamplingStrategy));
        private SamplingRule _defaultRule;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedSamplingStrategy"/> class.
        /// </summary>
        public LocalizedSamplingStrategy()
        {
            InitWithDefaultSamplingRules();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedSamplingStrategy"/> class.
        /// </summary>
        /// <param name="path">Path to the file which sampling configuration.</param>
        public LocalizedSamplingStrategy(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.DebugFormat("Initializing with default sampling rules.");
                InitWithDefaultSamplingRules();
            }
            else
            {
                _logger.DebugFormat("Initializing with custom sampling configuration : {0}", path);
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    Init(stream);
                }
            }
        }

        /// <summary>
        /// Gets or sets the default sampling rule.
        /// </summary>
        public SamplingRule DefaultRule
        {
            get
            {
                return _defaultRule;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.ServiceName != null || value.HttpMethod != null || value.UrlPath != null ||
                    value.FixedTarget == -1 || value.Rate == -1d)
                {
                    throw new InvalidSamplingConfigurationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "You are either missing required field or including extra fields ({0}). \"fixed_target\" and \"rate\" are required.",
                            value));
                }

                // Enforce default rule to match all
                value.ServiceName = "*";
                value.UrlPath = "*";
                value.HttpMethod = "*";
                _defaultRule = value;
            }
        }

        /// <summary>
        /// Gets the list of sampling Rules.
        /// </summary>
        public IList<SamplingRule> Rules { get; private set; }

        /// <summary>
        /// Apply the default sampling rule to make the sample decision
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="path">The path of request.</param>
        /// <param name="method">The HTTP method.</param>
        /// <returns>
        /// The sample decision made for this call
        /// </returns>
        public SampleDecision Sample(string serviceName, string path, string method)
        {
            var firstMatchRule = Rules.FirstOrDefault(r => r.IsMatch(serviceName, path, method));

            if (firstMatchRule == null)
            {
                _logger.DebugFormat("Can't match a rule for serviceName = {0}, path = {1}, method = {2}", serviceName, path, method);
                return ApplyRule(DefaultRule);
            }

            _logger.DebugFormat("Found a matching rule : ({0}) for serviceName = {1}, path = {2}, method = {3}", firstMatchRule.ToString(), serviceName, path, method);
            return ApplyRule(firstMatchRule);
        }

        /// <summary>
        /// Apply the first matched sampling rule for the given request  to make the sample decision.
        /// The Rules are evaluated from lowest to highest rule ID.
        /// </summary>
        /// <param name="request">The Http request that a sample decision will be made against.</param>
        /// <returns>The sample decision made for the request.</returns>
        public SampleDecision Sample(HttpRequestMessage request)
        {
            string serviceName = request.Headers.Host;
            string url = request.RequestUri.AbsolutePath;
            string method = request.Method.Method;

            return Sample(serviceName, url, method);
        }

        private static SampleDecision ApplyRule(SamplingRule rule)
        {
            if (rule.RateLimiter.Request())
            {
                return SampleDecision.Sampled;
            }

            return ThreadSafeRandom.NextDouble() <= rule.Rate ? SampleDecision.Sampled : SampleDecision.NotSampled;
        }

        private void InitWithDefaultSamplingRules()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultSamplingConfigurationResourceName))
            {
                Init(stream);
            }
        }

        private void Init(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                SamplingConfiguration samplingConfiguration = JsonMapper.ToObject<SamplingConfiguration>(reader);
                if (samplingConfiguration == null)
                {
                    throw new InvalidSamplingConfigurationException("The provided sampling configuration had invalid JSON format and cannot be parsed correctly.");
                }

                if (samplingConfiguration.Default == null)
                {
                    throw new InvalidSamplingConfigurationException("No default sampling rule is provided. A default sampling rule is required.");
                }

                if (samplingConfiguration.Version != SupportedSamplingConfigurationVersion)
                {
                    throw new InvalidSamplingConfigurationException(
                        string.Format(
                            CultureInfo.InvariantCulture, 
                            "The version of provided sampling configuration is not supported. Provided version = {0}, supported version = {1}", 
                            samplingConfiguration.Version, 
                            SupportedSamplingConfigurationVersion));
                }

                DefaultRule = samplingConfiguration.Default;

                var rules = new List<SamplingRule>();
                if (samplingConfiguration.Rules != null)
                {
                    foreach (var rule in samplingConfiguration.Rules)
                    {
                        if (rule.ServiceName == null || rule.HttpMethod == null || rule.UrlPath == null ||
                            rule.FixedTarget == -1 || rule.Rate == -1d)
                        {
                            throw new InvalidSamplingConfigurationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    @"Missing required fields for sampling rules ({0}). ""service_name"", ""http_method"", ""url_path"", ""fixed_target"", ""rate"" are required.",
                                    rule));
                        }

                        rules.Add(rule);
                    }
                }

                this.Rules = rules;
            }
        }
    }
}
