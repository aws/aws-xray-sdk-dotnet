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
using System.Reflection;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Core.Sampling.Local
{
    /// <summary>
    /// This strategy loads the sample Rules from local JSON file, and make the sample decision locally
    /// according to the Rules.
    /// </summary>
    public class LocalizedSamplingStrategy : ISamplingStrategy
    {
        private const string DefaultSamplingConfigurationResourceName = "Amazon.XRay.Recorder.Core.Sampling.Local.DefaultSamplingRule.json";
        private int[] SupportedSamplingConfigurationVersion = {1,2};
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

                if (value.Host != null || value.HttpMethod != null || value.UrlPath != null ||
                    value.FixedTarget == -1 || value.Rate == -1d)
                {
                    throw new InvalidSamplingConfigurationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "You are either missing required field or including extra fields ({0}). \"fixed_target\" and \"rate\" are required.",
                            value));
                }

                // Enforce default rule to match all
                value.Host = "*";
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
        /// <param name="host">Name of the service.</param>
        /// <param name="path">The path of request.</param>
        /// <param name="method">The HTTP method.</param>
        /// <returns>
        /// The sample decision made for this call
        /// </returns>
        private SampleDecision Sample(string host, string path, string method)
        {
            var firstMatchRule = Rules.FirstOrDefault(r => r.IsMatch(host, path, method));

            if (firstMatchRule == null)
            {
                _logger.DebugFormat("Can't match a rule for host = {0}, path = {1}, method = {2}", host, path, method);
                return ApplyRule(DefaultRule);
            }

            _logger.DebugFormat("Found a matching rule : ({0}) for host = {1}, path = {2}, method = {3}", firstMatchRule.ToString(), host, path, method);
            return ApplyRule(firstMatchRule);
        }

        /// <summary>
        /// Perform sampling decison based on <see cref="SamplingInput"/>.
        /// </summary>
        /// <param name="input"> Instance of <see cref="SamplingInput"/>.</param>
        /// <returns>Instance of <see cref="SamplingResponse"/>.</returns>
        public SamplingResponse ShouldTrace(SamplingInput input)
        {
            SampleDecision sampleDecision = Sample(input.Host,input.Url,input.Method);
            return new SamplingResponse(sampleDecision);
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

                if ( !SupportedSamplingConfigurationVersion.Contains(samplingConfiguration.Version))
                {
                    throw new InvalidSamplingConfigurationException(
                        string.Format(
                            CultureInfo.InvariantCulture, 
                            "The version of provided sampling configuration is not supported. Provided version = {0}, supported versions = {1}", 
                            samplingConfiguration.Version, String.Join(", ", SupportedSamplingConfigurationVersion)));
                }

                DefaultRule = samplingConfiguration.Default;

                var rules = new List<SamplingRule>();
                if (samplingConfiguration.Rules != null)
                {
                    foreach (var rule in samplingConfiguration.Rules) // contains supported versions.
                    {
                        if (IsValidVersion1(samplingConfiguration, rule))
                        {
                            rule.Host = rule.ServiceName;
                        }
                        else 
                        {
                            ValidateVersion2(samplingConfiguration, rule); // rule.Host already parsed in rule.
                        }

                        rules.Add(rule);
                    }
                }

                Rules = rules;
            }
        }

        private bool IsValidVersion1(SamplingConfiguration samplingConfiguration, SamplingRule rule)
        {
            if (samplingConfiguration.Version == 1)
            {
                if (rule.ServiceName == null || rule.HttpMethod == null || rule.UrlPath == null ||
                rule.FixedTarget == -1 || rule.Rate == -1d || rule.Host != null)
                {
                    throw new InvalidSamplingConfigurationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"Missing required fields for sampling rules ({0}). ""service_name"", ""http_method"", ""url_path"", ""fixed_target"", ""rate"" are required.",
                            rule));
                }

                return true;
            }

            return false;
        }
        private void ValidateVersion2(SamplingConfiguration samplingConfiguration, SamplingRule rule)
        {
            if (rule.Host == null || rule.HttpMethod == null || rule.UrlPath == null ||
                             rule.FixedTarget == -1 || rule.Rate == -1d || rule.ServiceName != null)
            {
                throw new InvalidSamplingConfigurationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        @"Missing required fields for sampling rules ({0}). ""host"", ""http_method"", ""url_path"", ""fixed_target"", ""rate"" are required.",
                        rule));
            }
        }
    }
}
