//-----------------------------------------------------------------------------
// <copyright file="Entity.cs" company="Amazon.com">
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// Represents the common part for both Segment and Subsegment.
    /// </summary>
    [Serializable]
    public abstract class Entity
    {
        private const string DefaultMetadataNamespace = "default";
        private const int SegmentIdHexDigits = 16;  // Number of hex digits in segment id
        private readonly Lazy<List<Subsegment>> _lazySubsegments = new Lazy<List<Subsegment>>();
        private readonly Lazy<ConcurrentDictionary<string, object>> _lazyHttp = new Lazy<ConcurrentDictionary<string, object>>();
        private readonly Lazy<Annotations> _lazyAnnotations = new Lazy<Annotations>();
        private readonly Lazy<ConcurrentDictionary<string, string>> _lazySql = new Lazy<ConcurrentDictionary<string, string>>();
        private readonly Lazy<ConcurrentDictionary<string, IDictionary>> _lazyMetadata = new Lazy<ConcurrentDictionary<string, IDictionary>>();
        private readonly Lazy<ConcurrentDictionary<string, object>> _lazyAws = new Lazy<ConcurrentDictionary<string, object>>();

        private string _traceId;
        private string _id;
        private string _name;
        private string _parentId;
        private long _referenceCounter;      // Reference count

        /// <summary>
        /// Initializes a new instance of the <see cref="Entity"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Entity(string name)
        {
            Id = GenerateId();
            IsInProgress = true;
            this.Name = name;
            IncrementReferenceCounter();
        }

        /// <summary>
        /// Gets or sets the unique id for the trace.
        /// </summary>
        /// <exception cref="System.ArgumentException">Trace id is invalid. - value</exception>
        public string TraceId
        {
            get
            {
                return _traceId;
            }

            set
            {
                if (!Entities.TraceId.IsIdValid(value))
                {
                    throw new ArgumentException("Trace id is invalid.", "value");
                }

                _traceId = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique id of segment.
        /// </summary>
        /// <value>
        /// The unique for Entity.
        /// </value>
        /// <exception cref="System.ArgumentException">The id is invalid. - value</exception>
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (value!=null && !IsIdValid(value))
                {
                    throw new ArgumentException("The id is invalid.", "value");
                }

                _id = value;
            }
        }

        /// <summary>
        /// Gets or sets the start time of this segment with Unix time in seconds.
        /// </summary>
        public decimal StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of this segment with Unix time in seconds.
        /// </summary>
        public decimal EndTime { get; set; }

        /// <summary>
        /// Gets or sets the name of the service component.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        /// <exception cref="System.ArgumentNullException">Thrown when value is null.</exception>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
            }
        }
        
        /// <summary>
        /// Gets a readonly copy of the subsegment list.
        /// </summary>
        public List<Subsegment> Subsegments
        {
            get
            {
                return _lazySubsegments.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any subsegments have been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if there are subsegments added; otherwise, <c>false</c>.
        /// </value>
        public bool IsSubsegmentsAdded
        {
            get { return _lazySubsegments.IsValueCreated && _lazySubsegments.Value.Any(); }
        }

        /// <summary>
        /// Gets or sets the unique id of upstream segment
        /// </summary>
        /// <value>
        /// The unique id for parent Entity.
        /// </value>
        /// <exception cref="System.ArgumentException">The parent id is invalid. - value</exception>
        public string ParentId
        {
            get
            {
                return _parentId;
            }

            set
            {
                if (!IsIdValid(value))
                {
                    throw new ArgumentException("The parent id is invalid.", "value");
                }

                _parentId = value;
            }
        }

        /// <summary>
        /// Gets the annotations of the segment
        /// </summary>
        public Annotations Annotations
        {
            get
            {
                return _lazyAnnotations.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any annotations have been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if annotations have been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsAnnotationsAdded
        {
            get
            {
                return _lazyAnnotations.IsValueCreated && _lazyAnnotations.Value.Any();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the segment has faulted or failed
        /// </summary>
        public bool HasFault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the segment has errored
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the remote segment is throttled
        /// </summary>
        public bool IsThrottled { get; set; }

        /// <summary>
        /// Gets the cause of fault or error
        /// </summary>
        public Cause Cause { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the segment is in progress
        /// </summary>
        public bool IsInProgress { get; set; }

        /// <summary>
        /// Gets reference of this instance of segment
        /// </summary>
        public long Reference
        {
            get
            {
                return Interlocked.Read(ref _referenceCounter);
            }
        }

        /// <summary>
        /// Gets the http attribute
        /// </summary>
        public IDictionary<string, object> Http
        {
            get
            {
                return _lazyHttp.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any HTTP information has been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if HTTP information has been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsHttpAdded
        {
            get
            {
                return _lazyHttp.IsValueCreated && !_lazyHttp.Value.IsEmpty;
            }
        }

        /// <summary>
        /// Gets the SQL information
        /// </summary>
        public IDictionary<string, string> Sql
        {
            get
            {
                return _lazySql.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any SQL information has been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if SQL information has been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsSqlAdded
        {
            get
            {
                return _lazySql.IsValueCreated && !_lazySql.Value.IsEmpty;
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public IDictionary<string, IDictionary> Metadata
        {
            get
            {
                return _lazyMetadata.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether any metadata has been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if metadata has been added; otherwise, <c>false</c>.
        /// </value>
        public bool IsMetadataAdded
        {
            get
            {
                return _lazyMetadata.IsValueCreated && !_lazyMetadata.Value.IsEmpty;
            }    
        }

        /// <summary>
        /// Gets or sets the sample decision
        /// </summary>
        public SampleDecision Sampled { get; set; }

        /// <summary>
        /// Gets or sets the root segment
        /// </summary>
        public Segment RootSegment { get; set; }

        /// <summary>
        /// Gets aws information
        /// </summary>
        public IDictionary<string, object> Aws
        {
            get
            {
                return _lazyAws.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether aws information has been added.
        /// </summary>
        /// <value>
        /// <c>true</c> if aws information has added; otherwise, <c>false</c>.
        /// </value>
        public bool IsAwsAdded
        {
            get
            {
                return _lazyAws.IsValueCreated && !_lazyAws.Value.IsEmpty;
            }
        }

        /// <summary>
        /// Validate the segment id
        /// </summary>
        /// <param name="id">The segment id to be validate</param>
        /// <returns>A value indicates if the id is valid</returns>
        public static bool IsIdValid(string id)
        {
            long tmp;
            return id.Length == SegmentIdHexDigits && long.TryParse(id, NumberStyles.HexNumber, null, out tmp);
        }

        /// <summary>
        /// Generates the id for entity.
        /// </summary>
        /// <returns>An id for entity.</returns>
        public static string GenerateId()
        {
            return ThreadSafeRandom.GenerateHexNumber(SegmentIdHexDigits);
        }

        /// <summary>
        /// Set start time of the segment to current time
        /// </summary>
        public void SetStartTimeToNow()
        {
            StartTime = DateTime.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Set end time of the segment to current time
        /// </summary>
        public void SetEndTimeToNow()
        {
            EndTime = DateTime.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Adds the specified key and value as annotation to current segment.
        /// The type of value is restricted. Only <see cref="string" />, <see cref="int" />, <see cref="long" />,
        /// <see cref="double" /> and <see cref="bool" /> are supported.
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The value of the annotation to add</param>
        /// <exception cref="System.ArgumentException">Key cannot be null or empty - key</exception>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="InvalidAnnotationException">The annotation to be added is invalid.</exception>
        public void AddAnnotation(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", "key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value is int)
            {
                _lazyAnnotations.Value.Add(key, (int)value);
                return;
            }

            if (value is long)
            {
                _lazyAnnotations.Value.Add(key, (long)value);
                return;
            }

            string stringValue = value as string;
            if (stringValue != null)
            {
                _lazyAnnotations.Value.Add(key, stringValue);
                return;
            }

            if (value is double)
            {
                _lazyAnnotations.Value.Add(key, (double)value);
                return;
            }

            if (value is bool)
            {
                _lazyAnnotations.Value.Add(key, (bool)value);
                return;
            }

            throw new InvalidAnnotationException(string.Format(CultureInfo.InvariantCulture, "Failed to add key={0}, value={1}, valueType={2} because the type is not supported.", key, value.ToString(), value.GetType().ToString()));
        }

        /// <summary>
        /// Add a subsegment
        /// </summary>
        /// <param name="subsegment">The subsegment to add</param>
        /// <exception cref="EntityNotAvailableException">Cannot add subsegment to a completed segment.</exception>
        public void AddSubsegment(Subsegment subsegment)
        {
            if (!IsInProgress)
            {
                throw new EntityNotAvailableException("Cannot add subsegment to a completed segment.");
            }

            lock (_lazySubsegments.Value)
            {
                _lazySubsegments.Value.Add(subsegment);
            }

            IncrementReferenceCounter();
            subsegment.Parent = this;
            subsegment.RootSegment = RootSegment;
            RootSegment.IncrementSize();
        }

        /// <summary>
        /// Adds the exception to cause and set this segment to has fault.
        /// </summary>
        /// <param name="e">The exception to be added.</param>
        public void AddException(Exception e)
        {
            HasFault = true;
            Cause = new Cause();
            Cause.AddException(AWSXRayRecorder.Instance.ExceptionSerializationStrategy.DescribeException(e, Subsegments));
        }

        /// <summary>
        /// Adds the specific key and value to metadata under default namespace.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddMetadata(string key, object value)
        {
            AddMetadata(DefaultMetadataNamespace, key, value);
        }

        /// <summary>
        /// Adds the specific key and value to metadata under given namespace.
        /// </summary>
        /// <param name="nameSpace">The name space.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddMetadata(string nameSpace, string key, object value)
        {
            _lazyMetadata.Value.GetOrAdd(nameSpace, new ConcurrentDictionary<string, object>()).Add(key, value);
        }

        /// <summary>
        /// Check if this segment or the root segment that this segment belongs to is ok to emit
        /// </summary>
        /// <returns>If the segment is ready to emit</returns>
        public abstract bool IsEmittable();

        /// <summary>
        /// Release reference to this instance of segment
        /// </summary>
        /// <returns>Reference count after release</returns>
        public abstract long Release();

        /// <summary>
        /// Release reference to this instance of segment
        /// </summary>
        /// <returns>Reference count after release</returns>
        protected long DecrementReferenceCounter()
        {
            return Interlocked.Decrement(ref _referenceCounter);
        }

        /// <summary>
        /// Add reference to this instance of segment
        /// </summary>
        /// <returns>Reference count after add</returns>
        protected long IncrementReferenceCounter()
        {
            return Interlocked.Increment(ref _referenceCounter);
        }

    }
}
