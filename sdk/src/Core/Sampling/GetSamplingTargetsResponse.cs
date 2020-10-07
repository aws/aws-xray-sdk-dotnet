//-----------------------------------------------------------------------------
// <copyright file="GetSamplingTargetsResponse.cs" company="Amazon.com">
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
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Wrapper for <see cref="AmazonXRayClient.GetSamplingTargetsAsync(Model.GetSamplingTargetsRequest, System.Threading.CancellationToken)"/> API call response.
    /// </summary>
    public class GetSamplingTargetsResponse
    {
        /// <summary>
        /// Instance of <see cref="GetSamplingTargetsResponse"/>.
        /// </summary>
        /// <param name="newTargets">List of <see cref="Target"/>.</param>
        public GetSamplingTargetsResponse(IList<Target> newTargets)
        {
            Targets = newTargets;
        }

        /// <summary>
        /// Sets Last rule modification timestamp returned by the GetSamplingTargets() API call.
        /// </summary>
        public TimeStamp RuleFreshness { get; set; }

        /// <summary>
        /// List of <see cref="Target"/>.
        /// </summary>
        public IList<Target> Targets { get; set; }
    }
}