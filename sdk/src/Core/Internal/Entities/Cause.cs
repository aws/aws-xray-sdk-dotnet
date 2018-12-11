//-----------------------------------------------------------------------------
// <copyright file="Cause.cs" company="Amazon.com">
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// Present the cause of fault and error in Segment and subsegment
    /// </summary>
    [Serializable]
    public class Cause
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Cause"/> class.
        /// </summary>
        public Cause()
        {
        }

        /// <summary>
        /// List of <see cref="ExceptionDescriptor"/>
        /// </summary>
        private Lazy<List<ExceptionDescriptor>> _exceptions = new Lazy<List<ExceptionDescriptor>>();

        /// <summary>
        /// Gets the working directory
        /// </summary>
        public string WorkingDirectory { get;  private set; }

        /// <summary>
        /// Gets the paths
        /// </summary>
        public IList<string> Paths { get; private set; }

        /// <summary>
        /// Gets a read-only copy of the list of exception to the cause
        /// </summary>
        public ReadOnlyCollection<ExceptionDescriptor> ExceptionDescriptors
        {
            get
            {
                return _exceptions.IsValueCreated ? _exceptions.Value.AsReadOnly() : null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any exception is added.
        /// </summary>
        /// <value>
        /// <c>true</c> if exception has been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsExceptionAdded
        {
            get
            {
                return _exceptions.IsValueCreated && _exceptions.Value.Any();
            }
        }
       
        /// <summary>
        /// Add list of <see cref="ExceptionDescriptor"/> to cause instance.
        /// </summary>
        /// <param name="exceptionDescriptors">List of <see cref="ExceptionDescriptor"/>.</param>
        public void AddException(List<ExceptionDescriptor> exceptionDescriptors)
        {
            if (exceptionDescriptors != null)
            {
                WorkingDirectory = Directory.GetCurrentDirectory();
            }
            _exceptions.Value.AddRange(exceptionDescriptors);
        }
    }
}
