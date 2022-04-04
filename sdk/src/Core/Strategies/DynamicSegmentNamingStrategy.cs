//-----------------------------------------------------------------------------
// <copyright file="DynamicSegmentNamingStrategy.cs" company="Amazon.com">
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

using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Utils;
#if NETFRAMEWORK
using System.Net.Http;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Try to match the Host field from HTTP header first, if not match then use the fallback name as segment name.
    /// </summary>
    /// <see cref="Amazon.XRay.Recorder.Core.Strategies.SegmentNamingStrategy" />
    public class DynamicSegmentNamingStrategy : SegmentNamingStrategy
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(DynamicSegmentNamingStrategy));

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicSegmentNamingStrategy" /> class.
        /// </summary>
        /// <param name="fallbackSegmentName">Name of the fallback segment.</param>
        /// <param name="hostNamePattern">The host name pattern.</param>
        /// <exception cref="System.ArgumentException">
        /// fallbackSegmentName cannot be null or empty. - fallbackSegmentName
        /// or
        /// hostNamePattern cannot be null or empty. - hostNamePattern
        /// </exception>
        public DynamicSegmentNamingStrategy(string fallbackSegmentName, string hostNamePattern = "*")
        {
            if (string.IsNullOrEmpty(fallbackSegmentName))
            {
                throw new ArgumentException("fallbackSegmentName cannot be null or empty.", "fallbackSegmentName");
            }

            if (string.IsNullOrEmpty(hostNamePattern))
            {
                throw new ArgumentException("hostNamePattern cannot be null or empty.", "hostNamePattern");
            }

            this.FallbackSegmentName = fallbackSegmentName;
            string overrideName = GetSegmentNameFromEnvironmentVariable();

            if (!string.IsNullOrEmpty(overrideName))
            {
                _logger.DebugFormat("{0} is set, overriding segment name to: {1}.", SegmentNamingStrategy.EnvironmentVariableSegmentName, overrideName);
                this.FallbackSegmentName = overrideName;
            }

            this.HostNamePattern = hostNamePattern;
        }

        /// <summary>
        /// Gets or sets the host name pattern regex.
        /// </summary>
        public string HostNamePattern { get; set; }

        /// <summary>
        /// Gets or sets the name of the fallback segment.
        /// </summary>
        public string FallbackSegmentName { get; set; }

#if NETFRAMEWORK
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/> request.</param>
        /// <returns>
        /// The segment name.
        /// </returns>
        public override string GetSegmentName(HttpRequestMessage httpRequest)
        {
            string hostField = httpRequest.Headers.Host;
            return ValidateHostField(hostField);
        }

        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>
        /// The segment name.
        /// </returns>
        public override string GetSegmentName(System.Web.HttpRequest httpRequest)
        {
            string hostField = httpRequest.Headers.Get("Host");
            return ValidateHostField(hostField);
        }

        private string ValidateHostField(string hostField)
        {
            try
            {
                if (hostField != null && hostField.WildcardMatch(HostNamePattern))
                {
                    return hostField;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Debug(e, "Timed out when matching host name to get segment name. Host: {0}", hostField);
            }

            return FallbackSegmentName;
        }

#else
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>
        /// The segment name.
        /// </returns>
        public override string GetSegmentName(HttpRequest httpRequest)
        {
            string hostField = httpRequest.Host.Host;
            try
            {
                if (hostField != null && hostField.WildcardMatch(HostNamePattern))
                {
                    return hostField;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Debug(e, "Timed out when matching host name to get segment name. Host: {0}", hostField);
            }

            return FallbackSegmentName;
        }
#endif
    }
}
