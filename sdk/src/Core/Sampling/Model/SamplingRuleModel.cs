//-----------------------------------------------------------------------------
// <copyright file="SamplingRuleModel.cs" company="Amazon.com">
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
    /// Class model for unmarshall sampling rule from sampling rule response json.
    /// </summary>
    public class SamplingRuleModel
    {
        public string RuleName;

        public int? Priority;

        public double? FixedRate;

        public int? ReservoirSize;

        public string Host;

        public string ServiceName;

        public string HTTPMethod;

        public string URLPath;

        public string ServiceType;

        public string ResourceARN;

        public string RuleARN;

        public int? Version;

        public Dictionary<string, string> Attributes;
    }
}
