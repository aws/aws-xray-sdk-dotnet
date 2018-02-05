//-----------------------------------------------------------------------------
// <copyright file="AWSOperationHandler.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Handlers.AwsSdk.Entities
{
    /// <summary>
    /// Handler for AWS services operation. It lists the information to be collected
    /// for the operation from request and response.
    /// </summary>
    public class AWSOperationHandler
    {
        /// <summary>
        /// Gets or sets the request parameters
        /// </summary>
        public List<string> RequestParameters { get; set; }

        /// <summary>
        /// Gets or sets the response parameters
        /// </summary>
        public List<string> ResponseParameters { get; set; }

        /// <summary>
        /// Gets or sets the request descriptors
        /// </summary>
        public Dictionary<string, AWSOperationRequestDescriptor> RequestDescriptors { get; set; }

        /// <summary>
        /// Gets or sets the response descriptors
        /// </summary>
        public Dictionary<string, AWSOperationResponseDescriptor> ResponseDescriptors { get; set; }
    }
}
