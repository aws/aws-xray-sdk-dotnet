//-----------------------------------------------------------------------------
// <copyright file="AWSServiceHandlerManifest.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Handlers.AwsSdk.Entities
{
    /// <summary>
    /// Manifest of AWS Service Handler.
    /// </summary>
    public class AWSServiceHandlerManifest
    {
        private Dictionary<string, AWSServiceHandler> _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSServiceHandlerManifest"/> class.
        /// </summary>
        public AWSServiceHandlerManifest()
        {
            _services = new Dictionary<string, AWSServiceHandler>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the map of service name to AwsServiceInfo. The key of map ignores case.
        /// </summary>
        public Dictionary<string, AWSServiceHandler> Services
        {
            get
            {
                return _services;
            }

            set
            {
                _services = value;
            }
        }
    }
}
