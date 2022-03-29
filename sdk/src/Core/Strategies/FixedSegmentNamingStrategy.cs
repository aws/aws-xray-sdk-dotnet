//-----------------------------------------------------------------------------
// <copyright file="FixedSegmentNamingStrategy.cs" company="Amazon.com">
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

using Amazon.Runtime.Internal.Util;
#if NET45
using System.Net.Http;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Use a fixed name as segment name.
    /// </summary>
    /// <seealso cref="Amazon.XRay.Recorder.Core.Strategies.SegmentNamingStrategy" />
    [CLSCompliant(false)]
    public class FixedSegmentNamingStrategy : SegmentNamingStrategy
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(FixedSegmentNamingStrategy));

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSegmentNamingStrategy"/> class.
        /// </summary>
        /// <param name="fixedName">Name of the fixed.</param>
        /// <exception cref="System.ArgumentNullException">fixedName is null.</exception>
        public FixedSegmentNamingStrategy(string fixedName)
        {
            if (string.IsNullOrEmpty(fixedName))
            {
                throw new ArgumentException("FixedName cannot be null or empty.", "fixedName");
            }

            this.FixedName = fixedName;
            string overrideName = GetSegmentNameFromEnvironmentVariable();
            if (!string.IsNullOrEmpty(overrideName))
            {
                _logger.DebugFormat("{0} is set, overriding segment name to: {1}.", SegmentNamingStrategy.EnvironmentVariableSegmentName, overrideName);
                this.FixedName = overrideName;
            }
        }

        /// <summary>
        /// Gets or sets the name of the fixed.
        /// </summary>
        public string FixedName { get; set; }

#if NET45
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/> request.</param>
        /// <returns>
        /// The segment name
        /// </returns>
        public override string GetSegmentName(HttpRequestMessage httpRequest)
        {
            return FixedName;
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
            return FixedName;
        }
#else
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>
        /// The segment name.
        /// </returns>
        [CLSCompliant(false)]
        public override string GetSegmentName(HttpRequest httpRequest)
        {
            return FixedName;
        }
#endif
    }
}
