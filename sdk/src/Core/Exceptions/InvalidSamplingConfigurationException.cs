//-----------------------------------------------------------------------------
// <copyright file="InvalidSamplingConfigurationException.cs" company="Amazon.com">
//      Copyright 2017 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

namespace Amazon.XRay.Recorder.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an invalid sampling configuration is seen.
    /// </summary>
    /// <see cref="Exception"/>
    public class InvalidSamplingConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSamplingConfigurationException"/> class.
        /// </summary>
        public InvalidSamplingConfigurationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSamplingConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidSamplingConfigurationException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSamplingConfigurationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner exception.</param>
        public InvalidSamplingConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
