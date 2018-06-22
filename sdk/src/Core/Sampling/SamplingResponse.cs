//-----------------------------------------------------------------------------
// <copyright file="SamplingResponse.cs" company="Amazon.com">
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
namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Sampling result returned by ShoudTrace().
    /// </summary>
    public class SamplingResponse
    {
        /// <summary>
        /// Enum of <see cref="Sampling.SampleDecision"/>.
        /// </summary>
        public SampleDecision SampleDecision { get; set; }

        /// <summary>
        /// Rule name for the the sample decision.
        /// </summary>
        public string RuleName { get; set; }

        public SamplingResponse()
        {
            SampleDecision = SampleDecision.Sampled;
        }

        public SamplingResponse(string ruleName, SampleDecision sampleDecision) : this(sampleDecision)
        {
            RuleName = ruleName;
        }

        public SamplingResponse(SampleDecision sampleDecision)
        {
            SampleDecision = sampleDecision;
        }
    }
}
