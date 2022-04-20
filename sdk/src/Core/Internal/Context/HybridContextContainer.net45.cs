//-----------------------------------------------------------------------------
// <copyright file="TraceContext.net45.cs" company="Amazon.com">
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Remoting.Messaging;
using System.Web;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.XRay.Recorder.Core.Internal.Context
{
    /// <summary>
    /// This context is a hybrid context of  <see cref="CallContext"/> and <see cref="HttpContext"/>, used for ASP.NET middleware. 
    /// The segment created by AWS X-Ray ASP.NET middleware is stored in CallContext and HttpContext. CRUD operations are performed only on CallContext. Subsegments created 
    /// along the lifecycle of the request are stored in CallContext.
    /// On <see cref="GetEntity"/> operation, if the value is not present in CallContext, the CallContext is populated with the segment that is stored in HttpContext.
    /// </summary>
    public class HybridContextContainer : TraceContextImpl
    {

        /// <summary>
        /// Key to store Entity in <see cref="HttpContext"/>.
        /// </summary>
        public const String XRayEntity = "XRayEntity";

        /// <summary>
        /// The default trace context used for CRUD operations.
        /// </summary>
        private ITraceContext _defaultContext = new CallContextContainer();

        /// <summary>
        /// Gets entity (segment/subsegment) from the call context. If entity is not present in call context, it populates the callcontext with the entity from <see cref="HttpContext"/>.
        /// </summary>
        /// <returns>The entity (segment/subsegment)</returns>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to get.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "It's a wrapper for CallContext.LogicalGetData().")]
        public override Entity GetEntity()
        {
            if (_defaultContext.IsEntityPresent()) // If CallContext has entity, return the entity 
            {
                return _defaultContext.GetEntity();
            }

            // If CallContext has no value, set the entity from HTTPContext into CallContext
            Entity entity = InjectEntityInTraceContext();
            return entity;
        }

        /// <summary>
        /// Set the specified entity (segment/subsegment) into call context. If the entity is segment, its also stored in <see cref="HttpContext"/>.(HTTPContext.Items[XRayEntity])
        /// </summary>
        /// <param name="entity">The segment to be set</param>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to set</exception>
        public override void SetEntity(Entity entity)
        {
            _defaultContext.SetEntity(entity);

            // If the entity is segment, store it in HTTPContext.Items[XRayEntity]
            Segment segment = entity as Segment;
            if (segment != null)
            {
                StoreEntityInHTTPContext(segment);
            }
        }

        /// <summary>
        /// Checks whether entity is present in CallContext. If not, check in HttpContext
        /// </summary>
        /// <returns>True if entity is present in <see cref="CallContext"/> or <see cref="HttpContext"/> else false.</returns>
        public override bool IsEntityPresent()
        {
            if(_defaultContext.IsEntityPresent()) 
            {
                return true;
            }

            var entity = GetEntityFromHTTPContext(); // If entity not present in CallContext, check in HTTPContext.
            return entity != null;
        }

        /// <summary>
        /// Clear entity from CallContext and HTTPContext for cleanup.
        /// </summary>
        public override void ClearEntity()
        {
            _defaultContext.ClearEntity();

            HttpContext httpContext = GetHTTPContext();
            
            if (httpContext != null)
            {
                httpContext.Items.Remove(XRayEntity);
            }
        }

        /// <summary>
        /// Gets instance of <see cref="HttpContext"/>
        /// </summary>
        /// <returns>Instance of <see cref="HttpContext"/>.</returns>
        private static HttpContext GetHTTPContext()
        {
            return HttpContext.Current;
        }

        /// <summary>
        /// Gets segment from <see cref="HttpContext"/> if available, else null.
        /// </summary>
        /// <returns>Entity from context.Items[XRayEntity </returns>
        private static Entity GetEntityFromHTTPContext()
        {
            try
            {
                HttpContext context = GetHTTPContext();
                if (context != null)
                {
                    return (Segment) context.Items[XRayEntity]; // strict check, only segment should be present
                }
            }
            catch (InvalidCastException e)
            {
                throw new EntityNotAvailableException("The data in HTTPContext is not a valid entity.", e);
            }

            return null;
        }

        /// <summary>
        /// Gets Segment from <see cref="HttpContext"/> and sets it to <see cref="CallContext"/>.
        /// </summary>
        /// <returns>Segment that is populated in CallContext.</returns>
        public Entity InjectEntityInTraceContext()
        {
            Entity entity = GetEntityFromHTTPContext();

            if (entity == null)
            {
                throw new EntityNotAvailableException("Entity doesn't exist in HTTPContext");
            }
            _defaultContext.SetEntity(entity);

            return entity;
        }

        /// <summary>
        /// Populates <see cref="HttpContext"/> with the segment.
        /// </summary>
        /// <param name="segment">Segment that is stored in HTTPContext</param>
        private static void StoreEntityInHTTPContext(Segment segment)
        {
            HttpContext context = GetHTTPContext();
            if (context != null)
            {
                context.Items[XRayEntity] = segment;
            }
        }
    }
}
