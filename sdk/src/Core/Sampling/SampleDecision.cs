//-----------------------------------------------------------------------------
// <copyright file="SampleDecision.cs" company="Amazon.com">
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
    /// Decisions for sampling
    /// </summary>
    public enum SampleDecision
    {
        /// <summary>
        /// Decision unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Tracing is enabled
        /// </summary>
        Sampled = '1',

        /// <summary>
        /// Tracing is disabled
        /// </summary>
        NotSampled = '0',

        /// <summary>
        /// The decision is not made, but need to be made
        /// </summary>
        Requested = '?'
    }
}
