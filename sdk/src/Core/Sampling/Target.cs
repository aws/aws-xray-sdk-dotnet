//-----------------------------------------------------------------------------
// <copyright file="Target.cs" company="Amazon.com">
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
using System;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Stores records received from GetSamplingTargets API call.
    /// </summary>
   public class Target
    {
        /// <summary>
        /// Fixed rate for the rule.
        /// </summary>
        public double FixedRate;

        /// <summary>
        /// Reservoir quota for the rule.
        /// </summary>
        public int? ReservoirQuota;

        /// <summary>
        /// TTL for the rule.
        /// </summary>
        public TimeStamp TTL;

        /// <summary>
        /// Rule name.
        /// </summary>
        public string RuleName;

        /// <summary>
        /// Interval for the rule.
        /// </summary>
        public int? Interval;
        public Target(string ruleName, double fixedRate, int? reservoirQuota, DateTime? ttl, int? interval)
        {
            RuleName = ruleName;
            FixedRate = fixedRate;
            ReservoirQuota = reservoirQuota;
            TTL = new TimeStamp(ttl);
            Interval = interval;
        }
    }
}
