﻿//-----------------------------------------------------------------------------
// <copyright file="XRayConfig.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Class for xray configuration for getting sampling rules and sampling targets.
    /// </summary>
    public class XRayConfig
    {
        /// <summary>
        /// Gets and sets of the ServiceURL property.
        /// </summary>
        public string ServiceURL { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public XRayConfig()
        {
        }
    }
}
