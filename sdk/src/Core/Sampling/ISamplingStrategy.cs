//-----------------------------------------------------------------------------
// <copyright file="ISamplingStrategy.cs" company="Amazon.com">
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

using System.Net.Http;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Interface of sampling strategy which is used to determine if tracing will be
    /// enabled for a given request.
    /// </summary>
    public interface ISamplingStrategy
    {
        /// <summary>
        /// Apply the default sampling rule to make the sample decision
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="path">The path of request.</param>
        /// <param name="method">The HTTP method.</param>
        /// <returns>
        /// The sample decision made for this call
        /// </returns>
        SampleDecision Sample(string serviceName, string path, string method);

        /// <summary>
        /// Apply the first matched sampling rule for the given request to make the sample decision. The evaluation order will be determined by the implementation.
        /// </summary>
        /// <param name="request">The Http request that a sample decision will be made against.</param>
        /// <returns>The sample decision made for the request.</returns>
        SampleDecision Sample(HttpRequestMessage request);
    }
}
