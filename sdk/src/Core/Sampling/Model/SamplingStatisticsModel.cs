//-----------------------------------------------------------------------------
// <copyright file="SamplingStatisticsModel.cs" company="Amazon.com">
//      Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

namespace Amazon.XRay.Recorder.Core.Sampling.Model
{
    /// <summary>
    /// Class for marshalling sampling statistics document json.
    /// </summary>
    public class SamplingStatisticsModel
    {
        public List<SamplingStatisticsDocumentModel> SamplingStatisticsDocuments { get; set; } = new List<SamplingStatisticsDocumentModel>();
    }
}
