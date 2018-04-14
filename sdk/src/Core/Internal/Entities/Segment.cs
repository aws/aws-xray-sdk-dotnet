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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
        public Segment(string name, string traceId=null, string parentId = null) : base(name)
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
        /// Sets start time of the segment to the provided timestamp.
        /// </summary>
        public void SetStartTime(decimal timestamp)
        {
            StartTime = timestamp;
        }
        /// <summary>
        /// Sets end time of the segment to the provided timestamp.
        /// </summary>
        public void SetEndTime(decimal timestamp)
        {
            EndTime = timestamp;
        }
    }
}
