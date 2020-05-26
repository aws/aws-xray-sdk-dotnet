//-----------------------------------------------------------------------------
// <copyright file="CallContextContainer.net45.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Remoting.Messaging;
using System.Security;
using Amazon.XRay.Recorder.Core.Exceptions;

namespace Amazon.XRay.Recorder.Core.Internal.Context
{
    public class CallContextContainer : TraceContextImpl
    {
        private const string Key = "AWSXRayRecorder";

        /// <summary>
        /// Get entity (segment/subsegment) from the context
        /// </summary>
        /// <returns>The segment get from context</returns>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to get.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It's a wrapper for CallContext.LogicalGetData().")]
        public override Entity GetEntity()
        {
            try
            {
                Entity entity = (Entity)CallContext.LogicalGetData(Key);
                if (entity == null)
                {
                    throw new EntityNotAvailableException("Entity doesn't exist in CallContext");
                }

                return entity;
            }
            catch (InvalidCastException e)
            {
                throw new EntityNotAvailableException("The data in CallContext is not a valid entity.", e);
            }
            catch (SecurityException e)
            {
                throw new EntityNotAvailableException("Caller does not have enough permission to get entity.", e);
            }
        }

        /// <summary>
        /// Set the specified entity (segment/subsegment) into context
        /// </summary>
        /// <param name="entity">The segment to be set</param>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to set</exception>
        public override void SetEntity(Entity entity)
        {
            try
            {
                CallContext.LogicalSetData(Key, entity);
            }
            catch (SecurityException e)
            {
                throw new EntityNotAvailableException("Caller does not have enough permission to set entity.", e);
            }
        }

        /// <summary>
        /// Clear entity from trace context for cleanup.
        /// </summary>
        public override void ClearEntity()
        {
            CallContext.FreeNamedDataSlot(Key);
        }

        /// <summary>
        /// Checks whether entity is present in <see cref="CallContext"/>.
        /// </summary>
        /// <returns>True if entity is present in <see cref="CallContext"/> else false.</returns>
        public override Boolean IsEntityPresent()
        {
            Entity entity = (Entity)CallContext.LogicalGetData(Key);

            if (entity == null)
            {
                return false;
            }

            return true;
        }
    }
}
