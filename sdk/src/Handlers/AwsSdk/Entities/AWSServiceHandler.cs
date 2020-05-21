//-----------------------------------------------------------------------------
// <copyright file="AWSServiceHandler.cs" company="Amazon.com">
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
    /// Handler for an AWS service. It contains a map of operation and its handler.
    /// </summary>
    public class AWSServiceHandler
    {
        private Dictionary<string, AWSOperationHandler> _operations;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSServiceHandler"/> class.
        /// </summary>
        public AWSServiceHandler()
        {
            _operations = new Dictionary<string, AWSOperationHandler>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the operations for the services
        /// </summary>
        public Dictionary<string, AWSOperationHandler> Operations
        {
            get
            {
                return _operations;
            }

            set
            {
                _operations = value;
            }
        }
    }
}
