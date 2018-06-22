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

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Interface of sampling strategy which is used to determine if tracing will be
    /// enabled for a given request.
    /// </summary>
    public interface ISamplingStrategy
    {
        /// <summary>
        /// Apply the first matched sampling rule for the given input to make the sample decision. The evaluation order will be determined by the implementation.
        /// </summary>
        /// <param name="input">An instance of <see cref="SamplingInput"/>.</param>
        /// <returns>The <see cref="SamplingResponse"/> which contains sampling decision and rule name made for the request.</returns>
        SamplingResponse ShouldTrace(SamplingInput input);
    }
}
