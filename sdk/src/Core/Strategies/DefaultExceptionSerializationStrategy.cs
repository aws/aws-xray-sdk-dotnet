//-----------------------------------------------------------------------------
// <copyright file="DefaultExceptionSerializationStrategy.cs" company="Amazon.com">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Defines default startegy for recording exception. By default <see cref="AmazonServiceException"/> class exeptions are marked as remote. 
    /// </summary>
    [Serializable]
    public class DefaultExceptionSerializationStrategy : ExceptionSerializationStrategy
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(DefaultExceptionSerializationStrategy));

        private static List<Type> _defaultExceptionClasses = new List<Type>() { typeof(AmazonServiceException)};

        private List<Type> _remoteExceptionClasses = new List<Type>();

        /// <summary>
        /// Default stack frame size for the recorded <see cref="Exception"/>.
        /// </summary>
        public const int DefaultStackFrameSize = 50;

        /// <summary>
        /// The maximum stack frame size for the strategy.
        /// </summary>
        public int MaxStackFrameSize { get; private set; } = DefaultStackFrameSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="_defaultExceptionClasses"/> class.
        /// </summary>
        public DefaultExceptionSerializationStrategy() : this(DefaultStackFrameSize)
        {
        }

        /// <summary>
        /// Initializes <see cref="DefaultExceptionSerializationStrategy"/> instance with provided Stack frame size. 
        /// While setting number consider max trace size limit : https://aws.amazon.com/xray/pricing/
        /// </summary>
        /// <param name="stackFrameSize">Integer value for stack frame size.</param>
        public DefaultExceptionSerializationStrategy(int stackFrameSize) 
        {
            MaxStackFrameSize = GetValidStackFrameSize(stackFrameSize);
            _remoteExceptionClasses.AddRange(_defaultExceptionClasses);
        }

        /// <summary>
        /// Initializes <see cref="DefaultExceptionSerializationStrategy"/> instance with provided Stack frame size and 
        /// list of types for which exceptions should be marked as remote.
        /// While setting number consider max trace size limit : https://aws.amazon.com/xray/pricing/
        /// </summary>
        /// <param name="stackFrameSize">Stack frame size for the recorded exception.</param>
        /// <param name="types">List of <see cref="Type"/> for which exceptions should be marked as remote.</param>
        public DefaultExceptionSerializationStrategy(int stackFrameSize, List<Type> types)
        {
            MaxStackFrameSize = GetValidStackFrameSize(stackFrameSize);
            _remoteExceptionClasses.AddRange(types);
            _remoteExceptionClasses.AddRange(_defaultExceptionClasses);
        }

        /// <summary>
        /// Initializes <see cref="DefaultExceptionSerializationStrategy"/> instance with provided
        /// list of types for which exceptions should be marked as remote.
        /// </summary>
        /// <param name="types">List of <see cref="Type"/> for which exceptions should be marked as remote.</param>
        public DefaultExceptionSerializationStrategy(List<Type> types)
        {
            MaxStackFrameSize = DefaultStackFrameSize;
            _remoteExceptionClasses.AddRange(types);
            _remoteExceptionClasses.AddRange(_defaultExceptionClasses);
        }

        /// <summary>
        /// Validates and returns valid max stack frame size.
        /// </summary>
        public static int GetValidStackFrameSize(int stackFrameSize)
        {
            if (stackFrameSize < 0)
            {
                _logger.DebugFormat("Provided Stack frame size should be non-negative. Setting max stack frame size : {0}", DefaultStackFrameSize);
                return DefaultStackFrameSize;
            }

            _logger.DebugFormat("Setting max stack frame size : {0}", stackFrameSize);
            return stackFrameSize;
        }

        /// <summary>
        /// Checks whether the exception should be marked as remote.
        /// </summary>
        /// <param name="e">Instance of <see cref="Exception"/>.</param>
        /// <returns>True if the exception is of type present in <see cref="_remoteExceptionClasses"/>, else false.</returns>
        private bool IsRemoteException(Exception e)
        {
            foreach (Type t in _remoteExceptionClasses)
            {
                Type exceptionType = e.GetType();
                if (exceptionType == t || exceptionType.IsSubclassOf(t))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Visit each node in the cause chain. For each node:
        /// Determine if it has already been described in one of the child subsegments' causes. If so, link there.
        /// Otherwise, describe it and add it to the Cause and  returns the list of <see cref="ExceptionDescriptor"/>.
        /// </summary>
        /// <param name="e">The exception to be added</param>
        /// <param name="subsegments">The subsegments to search for existing exception descriptor.</param>
        /// <returns> List of <see cref="ExceptionDescriptor"/></returns>
        public List<ExceptionDescriptor> DescribeException(Exception e, IEnumerable<Subsegment> subsegments)
        {
            List<ExceptionDescriptor> result = new List<ExceptionDescriptor>();
           
            // First check if the exception has been described in subsegment
            ExceptionDescriptor ex = new ExceptionDescriptor();
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

            // While referencing exception from child, record id if exists or cause and return.
            if (existingDescriptor != null)
            {
                ex.Cause = existingDescriptor.Id != null ? existingDescriptor.Id : existingDescriptor.Cause;
                ex.Exception = existingDescriptor.Exception; // pass the exception of the cause so that this reference can be found if the same exception is thrown again
                ex.Id = null;  // setting this to null since, cause is already populated with reference to downstream exception
                result.Add(ex);
                return result;
            }

            // The exception is not described. Start describe it.
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

                if (IsRemoteException(e))
                {
                    curDescriptor.Remote = true;
                }

                result.Add(curDescriptor);

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

            return result;
        }
    }
}
