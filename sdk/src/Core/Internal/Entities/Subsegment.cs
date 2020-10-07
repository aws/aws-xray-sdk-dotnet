//-----------------------------------------------------------------------------
// <copyright file="Subsegment.cs" company="Amazon.com">
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
using System.Linq;
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// A trace subsegment tracks unit of computation within a trace segment (e.g. a method or function) or a downstream call.
    /// </summary>
    /// <seealso cref="Amazon.XRay.Recorder.Core.Internal.Entities.Entity" />
    [Serializable]
    public class Subsegment : Entity
    {
        private readonly Lazy<HashSet<string>> _lazyPrecursorIds = new Lazy<HashSet<string>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Subsegment"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Subsegment(string name) : base(name)
        {
        }

        /// <summary>
        /// Gets or sets the namespace of the subsegment
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets parent segment
        /// </summary>
        public Entity Parent { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets the precursor ids
        /// </summary>
        public IEnumerable<string> PrecursorIds
        {
            get
            {
                return _lazyPrecursorIds.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether precursor is has been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if precursor id has been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrecursorIdAdded
        {
            get
            {
                return _lazyPrecursorIds.IsValueCreated && _lazyPrecursorIds.Value.Any();
            }
        }

        /// <summary>
        /// Add the given precursor id to a set
        /// </summary>
        /// <param name="precursorId">The precursor id to add to the set</param>
        /// <returns>true if the id is added; false if the id is already present.</returns>
        /// <exception cref="ArgumentException">The given precursor id is not a valid segment id.</exception>
        public bool AddPrecursorId(string precursorId)
        {
            if (!Entity.IsIdValid(precursorId))
            {
                throw new ArgumentException("The precursor id is not a valid segment id: ", precursorId);
            }

            bool ret;
            lock (_lazyPrecursorIds.Value)
            {
                ret = _lazyPrecursorIds.Value.Add(precursorId);
            }

            return ret;
        }

        /// <summary>
        /// Check if this segment or the root segment that this segment belongs to is ok to emit
        /// </summary>
        /// <returns>If the segment is ready to emit</returns>
        public override bool IsEmittable()
        {
            return Reference == 0 && Parent != null && Parent.IsEmittable();
        }

        /// <summary>
        /// Release reference to this instance of segment
        /// </summary>
        /// <returns>Reference count after release</returns>
        public override long Release()
        {
            long count = DecrementReferenceCounter();
            if (count == 0 && Parent != null)
            {
                Parent.Release();
            }

            return count;
        }
    }
}
