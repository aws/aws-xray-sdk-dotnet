//-----------------------------------------------------------------------------
// <copyright file="IConnector.cs" company="Amazon.com">
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
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Interface for API calls to X-Ray service.
    /// </summary>
    internal interface IConnector
    {
        Task<GetSamplingRulesResponse> GetSamplingRules();
        Task<GetSamplingTargetsResponse> GetSamplingTargets(List<SamplingRule> rules);
    }
}
