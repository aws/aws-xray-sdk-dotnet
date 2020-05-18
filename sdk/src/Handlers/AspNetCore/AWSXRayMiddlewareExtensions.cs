//-----------------------------------------------------------------------------
// <copyright file="AWSXRayMiddlewareExtensions.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Strategies;
using Amazon.XRay.Recorder.Handlers.AspNetCore.Internal;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// The Middleware Extension to intercept HTTP request for ASP.NET Core.
    /// For each request, <see cref="AWSXRayMiddleware"/> will try to parse trace header
    /// from HTTP request header, and determine if tracing is enabled. If enabled, it will
    /// start a new segment before invoking inner handler. And end the segment before it returns
    /// the response to outer handler.
    /// </summary>
    public static class AWSXRayMiddlewareExtensions
    {
        /// <summary>
        /// Adds <see cref="AWSXRayMiddleware"/> to the application's request pipeline.
        /// </summary>
        /// <param name="builder">Instance of <see cref="IApplicationBuilder"/>.</param>
        /// <param name="segmentName">Segment name.</param>
        /// <returns>Instance of <see cref="IApplicationBuilder"/> instrumented with X-Ray middleware.</returns>
        public static IApplicationBuilder UseXRay(this IApplicationBuilder builder, string segmentName)
        {
            return builder.UseMiddleware<AWSXRayMiddleware>(segmentName);
        }

        /// <summary>
        /// Adds <see cref="AWSXRayMiddleware"/> to the application's request pipeline.
        /// </summary>
        /// <param name="builder">Instance of <see cref="IApplicationBuilder"/></param>
        /// <param name="segmentName">Segment name.</param>
        /// <param name="configuration"></param>
        /// <returns>Instance of <see cref="IApplicationBuilder"/> instrumented with X-Ray middleware.</returns>
        public static IApplicationBuilder UseXRay(this IApplicationBuilder builder, string segmentName, IConfiguration configuration)
        {
            return builder.UseMiddleware<AWSXRayMiddleware>(segmentName, configuration);
        }

        /// <summary>
        /// Adds <see cref="AWSXRayMiddleware"/> to the application's request pipeline.
        /// </summary>
        /// <param name="builder">Instance of <see cref="IApplicationBuilder"/>.</param>
        /// <param name="segmentNamingStrategy"></param>
        /// <returns>Instance of <see cref="IApplicationBuilder"/> instrumented with X-Ray middleware.</returns>
        public static IApplicationBuilder UseXRay(this IApplicationBuilder builder, SegmentNamingStrategy segmentNamingStrategy)
        {
            return builder.UseMiddleware<AWSXRayMiddleware>(segmentNamingStrategy);
        }

        /// <summary>
        /// Adds <see cref="AWSXRayMiddleware"/> to the application's request pipeline.
        /// </summary>
        /// <param name="builder">Instance of <see cref="IApplicationBuilder"/>.</param>
        /// <param name="segmentNamingStrategy"></param>
        /// <param name="configuration"></param>
        /// <returns>Instance of <see cref="IApplicationBuilder"/> instrumented with X-Ray middleware.</returns>
        public static IApplicationBuilder UseXRay(this IApplicationBuilder builder, SegmentNamingStrategy segmentNamingStrategy, IConfiguration configuration)
        {
            return builder.UseMiddleware<AWSXRayMiddleware>(segmentNamingStrategy, configuration);
        }
    }
}
