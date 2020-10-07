//-----------------------------------------------------------------------------
// <copyright file="TraceContextImpl.cs" company="Amazon.com">
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
using System.Runtime.ExceptionServices;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Strategies;

namespace Amazon.XRay.Recorder.Core.Internal.Context
{
    public abstract class TraceContextImpl : ITraceContext
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(TraceContextImpl));

        /// <summary>
        /// Get entity (segment/subsegment) from the trace context.
        /// </summary>
        /// <returns>The segment get from context</returns>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to get.</exception>
       public abstract Entity GetEntity();

        /// <summary>
        /// Set the specified entity (segment/subsegment) into trace context.
        /// </summary>
        /// <param name="entity">The segment to be set</param>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to set</exception>
        public abstract void SetEntity(Entity entity);

        /// <summary>
        /// Clear entity from trace context for cleanup.
        /// </summary>
        public abstract void ClearEntity();

        /// <summary>
        /// Checks whether enity is present in trace context.
        /// </summary>
        /// <returns>True if entity is present incontext container else false.</returns>
        public abstract Boolean IsEntityPresent();

        /// <summary>
        /// If the entity is missing from the context, the behavior is defined using <see cref="ContextMissingStrategy"/>
        /// </summary>
        /// <param name="recorder"><see cref="IAWSXRayRecorder"/> instance</param>
        /// <param name="e">Instance of <see cref="Exception"/></param>
        /// <param name="message">String message</param>
        public void HandleEntityMissing(IAWSXRayRecorder recorder, Exception e, string message)
        {
            _logger.Error(e, message);

            if (recorder.ContextMissingStrategy == ContextMissingStrategy.LOG_ERROR)
            {
                _logger.DebugFormat("The ContextMissingStrategy is set to be LOG_ERROR. EntityNotAvailableException exception is suppressed.");
            }
            else
            {
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }
    }
}
