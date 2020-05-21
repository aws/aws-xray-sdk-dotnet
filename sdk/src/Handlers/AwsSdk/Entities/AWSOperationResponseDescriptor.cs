﻿//-----------------------------------------------------------------------------
// <copyright file="AWSOperationResponseDescriptor.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Handlers.AwsSdk.Entities
{
    /// <summary>
    /// Response descriptor for operation of AWS services. The difference between response descriptor
    /// and parameter is descriptor represents attribute with <see cref="List"/> type, and only the count
    /// of the list get collected.
    /// </summary>
    public class AWSOperationResponseDescriptor
    {
        /// <summary>
        /// Gets or sets the new name for the field
        /// </summary>
        public string RenameTo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the filed is a map
        /// </summary>
        public bool Map { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key should be get
        /// </summary>
        public bool GetKeys { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the filed is a list
        /// </summary>
        public bool List { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the count of the list should be get
        /// </summary>
        public bool GetCount { get; set; }
    }
}
