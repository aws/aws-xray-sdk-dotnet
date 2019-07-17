//-----------------------------------------------------------------------------
// <copyright file="Segment.cs" company="Amazon.com">
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

using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Sampling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Amazon.XRay.Recorder.Core.Exceptions;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// A trace segment tracks a period of time associated with a computation or action, along with annotations and key / value data.
    /// A set of trace segments all of which share the same tracing ID form a trace.
    /// </summary>
    /// <seealso cref="Amazon.XRay.Recorder.Core.Internal.Entities.Entity" />
    [Serializable]
    public class Segment : Entity
    {
        private long _size;           // Total number of subsegments
        private Lazy<ConcurrentDictionary<string, object>> _lazyService = new Lazy<ConcurrentDictionary<string, object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        /// <param name="name">Name of the node or service component.</param>
        /// <param name="traceId">Unique id for the trace.</param>
        /// <param name="parentId">Unique id of the upstream segment.</param>
        public Segment(string name, string traceId = null, string parentId = null) : base(name)
        {
            if (traceId != null)
            {
                this.TraceId = traceId;
            }
            else
            {
                this.TraceId = Entities.TraceId.NewId();
            }

            if (parentId != null)
            {
                this.ParentId = parentId;
            }

            RootSegment = this;
        }

        /// <summary>
        /// Gets or Sets the User for the segment
        /// </summary>
        public string User { get; set; }
       

        /// <summary>
        /// Gets or sets the origin of the segment.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Gets the size of subsegments.
        /// </summary>
        public long Size
        {
            get
            {
                return Interlocked.Read(ref _size);
            }
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        public IDictionary<string, object> Service
        {
            get
            {
                return _lazyService.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any value has been added to service.
        /// </summary>
        public bool IsServiceAdded
        {
            get
            {
                return _lazyService.IsValueCreated && !_lazyService.Value.IsEmpty;
            }
        }

        /// <summary>
        /// Increment the size count.
        /// </summary>
        public void IncrementSize()
        {
            Interlocked.Increment(ref _size);
        }

        /// <summary>
        /// Decrement the size count.
        /// </summary>
        public void DecrementSize()
        {
            Interlocked.Decrement(ref _size);
        }

        /// <summary>
        /// Release reference to this instance of segment.
        /// </summary>
        /// <returns>Reference count after release.</returns>
        public override long Release()
        {
            return DecrementReferenceCounter();
        }

        /// <summary>
        /// Check if this segment or the root segment that this segment belongs to is ok to emit.
        /// </summary>
        /// <returns>If the segment is ready to emit.</returns>
        public override bool IsEmittable()
        {
            return Reference == 0;
        }

        /// <summary>
        /// Checks if the segment has been streamed already
        /// </summary>
        /// <exception cref="AlreadyEmittedException">The segment has been already streamed and no further operation can be performed on it.</exception>
        private void HasAlreadyStreamed()
        {
            if(HasStreamed)
            {
                throw new AlreadyEmittedException("Segment " + Name + " has already been emitted.");
            }
        }

        /// <summary>
        /// Gets the value of the User for this segment
        /// </summary>
        public string GetUser()
        {
            return User;
        }

        /// <summary>
        /// Sets the User for this segment
        /// </summary>
        /// <param name="user">the name of the user</param>
        /// <exception cref="System.ArgumentNullException">The value of user cannot be null.</exception>
        public void SetUser(string user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            HasAlreadyStreamed();
            this.User = user;
        }
    }
}
