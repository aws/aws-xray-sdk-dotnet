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
using System.Diagnostics;
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
        /// The maximum stack frame size
        /// </summary>
        public const int MaxStackFrameSize = 50;

        private Lazy<List<ExceptionDescriptor>> _exceptions = new Lazy<List<ExceptionDescriptor>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Cause"/> class.
        /// </summary>
        public Cause()
        {
        }

        /// <summary>
        /// Gets or sets id of a reference exception id.
        /// If this id is set, the exception is already described in another segment.
        /// </summary>
        public string ReferenceExceptionId { get; set; }

        /// <summary>
        /// Gets the working directory
        /// </summary>
        public string WorkingDirectory { get; private set; }

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
        /// Add an exception to the cause
        /// </summary>
        /// <param name="e">The exception to be added</param>
        /// <param name="subsegments">The subsegments to search for existing exception descriptor.</param>
        public void AddException(Exception e, IEnumerable<Subsegment> subsegments)
        {
            // First check if the exception has been described in subsegments
            IEnumerable<ExceptionDescriptor> existingExceptionDescriptors = null;
            if (subsegments != null)
            {
                existingExceptionDescriptors = subsegments.Where(subsegment => subsegment.Cause != null && subsegment.Cause.IsExceptionAdded).SelectMany(subsegment => subsegment.Cause.ExceptionDescriptors);
            }

            ExceptionDescriptor existingDescriptor = null;
            if (existingExceptionDescriptors != null)
            {
                existingDescriptor = existingExceptionDescriptors.FirstOrDefault(descriptor => e.Equals(descriptor.Exception));
            }

            if (existingDescriptor != null)
            {
                ReferenceExceptionId = existingDescriptor.Id;
                return;
            }

            // The exception is not described. Start describe it.
            WorkingDirectory = Directory.GetCurrentDirectory();
            ExceptionDescriptor curDescriptor = new ExceptionDescriptor();

            while (e != null)
            {
                curDescriptor.Exception = e;
                curDescriptor.Message = e.Message;
                curDescriptor.Type = e.GetType().Name;
                StackFrame[] frames = new StackTrace(e, true).GetFrames();
                if (frames != null && frames.Length > MaxStackFrameSize)
                {
                    curDescriptor.Truncated = frames.Length - MaxStackFrameSize;
                    curDescriptor.Stack = new StackFrame[MaxStackFrameSize];
                    Array.Copy(frames, curDescriptor.Stack, MaxStackFrameSize);
                }
                else
                {
                    curDescriptor.Stack = frames;
                }

                _exceptions.Value.Add(curDescriptor);

                e = e.InnerException;
                if (e != null)
                {
                    // Inner exception alreay described
                    ExceptionDescriptor innerExceptionDescriptor = existingExceptionDescriptors != null ? existingExceptionDescriptors.FirstOrDefault(d => d.Exception.Equals(e)) : null;
                    if (innerExceptionDescriptor != null)
                    {
                        curDescriptor.Cause = innerExceptionDescriptor.Id;
                        e = null;
                    }
                    else
                    {
                        var newDescriptor = new ExceptionDescriptor();
                        curDescriptor.Cause = newDescriptor.Id;
                        curDescriptor = newDescriptor;
                    }
                }
            }
        }
    }
}
