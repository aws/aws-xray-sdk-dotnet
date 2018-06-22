//-----------------------------------------------------------------------------
// <copyright file="AWSXRaySDKUtils.cs" company="Amazon.com">
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
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.Handlers.AwsSdk.Internal
{
    /// <summary>
    /// Utility class for AWS SDK handler.
    /// </summary>
    internal class AWSXRaySDKUtils
    {
        private static readonly String XRayServiceName = "XRay";
        private static readonly ISet<String> WhitelistedOperations = new HashSet<String> { "GetSamplingRules", "GetSamplingTargets" };
        internal static bool IsBlacklistedOperation(String serviceName, string operation)
        {
            if (string.Equals(serviceName, XRayServiceName) && WhitelistedOperations.Contains(operation))
            {
                return true;
            }
            return false;
        }
    }
}
