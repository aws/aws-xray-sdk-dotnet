//-----------------------------------------------------------------------------
// <copyright file="SamplingRule.cs" company="Amazon.com">
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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Utils;

[module: SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes", Scope = "type", Target = "Amazon.XRay.Recorder.Core.Sampling.Local.SamplingRule", Justification = "Only used for sorting")]

namespace Amazon.XRay.Recorder.Core.Sampling.Local
{
    /// <summary>
    /// It represents the Rules used for sampling.
    /// </summary>    
    public class SamplingRule
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(SamplingRule));
        private int _fixedTarget;

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingRule"/> class.
        /// </summary>
        public SamplingRule()
        {
            _fixedTarget = -1;
            this.Rate = -1d;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingRule"/> class.
        /// </summary>
        /// <param name="host">Name of the host. The value can include a multi-character match wildcard(*) or a single-character match wildcard (?) anywhere in the string.</param>
        /// <param name="urlPath">The URL path. The value can include a multi-character match wildcard(*) or a single-character match wildcard (?) anywhere in the string.</param>
        /// <param name="httpMethod">Http method. The value can be a multi-character match wildcard(*) to match any method.</param>
        /// <param name="fixedTarget">It defines a trace collection target for a rule with no sampling in the unit of traces per second. Before the threshold is met, all request will be traced. After the threshold is met, sampling rate is triggered.</param>
        /// <param name="rate">The rate at which request will be sampled. E.g. with 5% sampling rate, average 5 request out of 100 will be traced.</param>
        /// <param name="description">Description of the sampling rule.</param>
        public SamplingRule(string host, string urlPath, string httpMethod, int fixedTarget, double rate, string description = null)
        {
            this.Host = host;
            this.HttpMethod = httpMethod;
            this.UrlPath = urlPath;
            this.FixedTarget = fixedTarget;
            this.Rate = rate;
            this.Description = description;
        }

        /// <summary>
        /// Gets or sets the host of the rule
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the service name from V1 sampling rule json file.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the http method of the rule
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets the url path of the rule
        /// </summary>
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets the fixed target rate of the rule in the unit of traces/second
        /// </summary>
        public int FixedTarget
        {
            get
            {
                return _fixedTarget;
            }

            set
            {
                _fixedTarget = value;
                RateLimiter = new RateLimiter(value);
            }
        }

        /// <summary>
        /// Gets the rate limiter which had the limit set to fixed target rate
        /// </summary>
        public RateLimiter RateLimiter { get; private set; }

        /// <summary>
        /// Gets or sets the sampling rate
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Given the service name, http method and url path of a http request, check whether the rule matches the request 
        /// </summary>
        /// <param name="hostToMatch">host name of the request</param>
        /// <param name="urlPathToMatch">url path of the request</param>
        /// <param name="httpMethodToMatch">http method of the request</param>
        /// <returns>It returns true if the rule matches the request, otherwise it returns false.</returns>
        public bool IsMatch(string hostToMatch, string urlPathToMatch, string httpMethodToMatch)
        {
            try
            {
                return StringExtension.IsMatch(hostToMatch, Host) && StringExtension.IsMatch(urlPathToMatch, UrlPath) && StringExtension.IsMatch(httpMethodToMatch, HttpMethod);
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Error(e, "Match rule timeout. Rule: Host = {0}, UrlPath = {1}, HttpMethod = {2}. Input: hostToMatch = {3}, urlPathToMatch = {4}, httpMethodToMatch = {5}.", Host, UrlPath, HttpMethod, hostToMatch, urlPathToMatch, httpMethodToMatch);
                return false;
            }
        }

        /// <summary>
        /// Generate a string out of this instance of the class
        /// </summary>
        /// <returns>The string generated from current object</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "hostToMatch={0}, httpMethodToMatch={1}, urlPathToMatch={2}, fixedTarget={3}, rate={4}, description={5}", Host, HttpMethod, UrlPath, FixedTarget, Rate, Description);
        }
    }
}
