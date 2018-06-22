//-----------------------------------------------------------------------------
// <copyright file="GetSamplingRulesResponse.cs" company="Amazon.com">
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
    /// Wrapper to <see cref="AmazonXRayClient.GetSamplingRulesAsync(Model.GetSamplingRulesRequest, System.Threading.CancellationToken)"/> API call response.
    /// </summary>
    public class GetSamplingRulesResponse
    {
        /// <summary>
        /// List of <see cref="SamplingRule"/>.
        /// </summary>
        public List<SamplingRule> Rules;

        /// <summary>
        /// Instance of <see cref="GetSamplingRulesResponse"/>.
        /// </summary>
        /// <param name="rules"> List of <see cref="SamplingRule"/>.</param>
        public GetSamplingRulesResponse(List<SamplingRule> rules)
        {
            Rules = rules;
        }

        internal bool IsRulePresent()
        {
            return Rules.Count > 0 ? true : false;
        }
    }
}