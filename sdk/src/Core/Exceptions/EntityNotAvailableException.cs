﻿//-----------------------------------------------------------------------------
// <copyright file="EntityNotAvailableException.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when segment is not available.
    /// </summary>
    [Serializable]
    public class EntityNotAvailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotAvailableException"/> class.
        /// </summary>
        public EntityNotAvailableException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotAvailableException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">Error message</param>
        public EntityNotAvailableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotAvailableException"/> class 
        /// with a specified error message and a reference to the inner exception that is 
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="inner">Inner exception</param>
        public EntityNotAvailableException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
