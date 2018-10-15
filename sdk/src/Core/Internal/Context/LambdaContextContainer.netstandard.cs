//-----------------------------------------------------------------------------
// <copyright file="LambdaContextContainer.netstandard.cs" company="Amazon.com">
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

using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Internal.Context
{
    /// <summary>
    /// This context is used in AWS Lambda environment.
    /// </summary>
    public class LambdaContextContainer : ITraceContext
    {
        private static AsyncLocal<Entity> _entityHolder = new AsyncLocal<Entity>();

        /// <summary>
        /// Get entity (segment/subsegment) from the context.
        /// </summary>
        /// <returns>The segment get from context.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It's a wrapper for AsyncLocal.")]
        public Entity GetEntity()
        {
            Entity entity = _entityHolder.Value;
            if (entity == null)
            {
                AWSXRayRecorder.Instance.AddFacadeSegment();
                entity = _entityHolder.Value;
            }

            return entity;
        }

        /// <summary>
        /// Set the specified entity (segment/subsegment) into context.
        /// </summary>
        /// <param name="entity">The segment to be set.</param>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to set.</exception>
        public void SetEntity(Entity entity)
        {
            _entityHolder.Value = entity;
        }

        /// <summary>
        /// Clear entity from trace context for cleanup.
        /// </summary>
        public void ClearEntity()
        {
            _entityHolder.Value = null;
        }

        /// <summary>
        /// Checks whether enity is present in <see cref="AsyncLocal{T}"/>.
        /// </summary>
        /// <returns>True if entity is present in <see cref="AsyncLocal{T}"/> else false.</returns>
        public Boolean IsEntityPresent()
        {
            Entity entity = _entityHolder.Value;

            if (entity == null)
            {
                return false;
            }

            return true;
        }
    }
}
