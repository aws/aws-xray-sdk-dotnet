//-----------------------------------------------------------------------------
// <copyright file="ExceptionDescriptor.cs" company="Amazon.com">
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// AWS X-Ray Descriptor of Exception
    /// </summary>
    [Serializable]
    public class ExceptionDescriptor
    {
        /// <summary>
        /// The exception descriptor identifier length
        /// </summary>
        public const int ExceptionDescriptorIdLength = 16;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionDescriptor"/> class.
        /// </summary>
        public ExceptionDescriptor()
        {
            Id = ThreadSafeRandom.GenerateHexNumber(ExceptionDescriptor.ExceptionDescriptorIdLength);
        }

        /// <summary>
        /// Gets or sets the id of the descriptor.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ExceptionDescriptor"/> is remove.
        /// </summary>
        public bool Remove { get; set; }

        /// <summary>
        /// Gets or sets the stack.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "The value of stack is not supposed to change after set.")]
        public StackFrame[] Stack { get; set; }

        /// <summary>
        /// Gets or sets the truncated.
        /// </summary>
        public int Truncated { get; set; }

        /// <summary>
        /// Gets or sets the cause.
        /// </summary>
        public string Cause { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
