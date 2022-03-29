//-----------------------------------------------------------------------------
// <copyright file="SegmentNamingStrategy.cs" company="Amazon.com">
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

#if NET45
using System.Net.Http;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Strategy to name a segment
    /// </summary>
    [CLSCompliant(false)]
    public abstract class SegmentNamingStrategy
    {
        /// <summary>
        /// The environment variable for segment name
        /// </summary>
        public const string EnvironmentVariableSegmentName = "AWS_XRAY_TRACING_NAME";

        /// <summary>
        /// Gets the segment name from environment variable.
        /// </summary>
        /// <returns>Segment name from environment variable</returns>
        public static string GetSegmentNameFromEnvironmentVariable()
        {
            return Environment.GetEnvironmentVariable(SegmentNamingStrategy.EnvironmentVariableSegmentName);
        }

#if NET45
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The segment name</returns>
        public abstract string GetSegmentName(HttpRequestMessage httpRequest);

        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The segment name</returns>
        public abstract string GetSegmentName(System.Web.HttpRequest httpRequest);

#else
        /// <summary>
        /// Gets the name of the segment.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/> request.</param>
        /// <returns>The segment name</returns>
        public abstract string GetSegmentName(HttpRequest httpRequest);
#endif
    }
}
