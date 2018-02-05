//-----------------------------------------------------------------------------
// <copyright file="AWSXRayRecorder.cs" company="Amazon.com">
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
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
namespace Amazon.XRay.Recorder.Core
{
    /// <summary>
    /// A collection of methods used to record tracing information for AWS X-Ray.
    /// </summary>
    /// <see cref="Amazon.XRay.Recorder.Core.IAWSXRayRecorder" />
    public class AWSXRayRecorder : AWSXRayRecorderImpl
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorder));

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class
        /// with default configuration.
        /// </summary>
        public AWSXRayRecorder() : this(new UdpSegmentEmitter())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class
        /// with given instance of <see cref="ISegmentEmitter" />.
        /// </summary>
        /// <param name="emitter">Segment emitter</param>
        internal AWSXRayRecorder(ISegmentEmitter emitter):base(emitter)
        {
            PopulateContexts();
            SamplingStrategy = new LocalizedSamplingStrategy(AppSettings.SamplingRuleManifest);
        }


        /// <summary>
        /// Gets the singleton instance of <see cref="AWSXRayRecorder"/> with default configuration.
        /// The default configuration uses <see cref="DefaultSamplingStrategy"/> and read all configuration
        /// value from AppSettings.
        /// </summary>
        /// <returns>An instance of <see cref="AWSXRayRecorder"/> class.</returns>
        public static AWSXRayRecorder Instance
        {
            get
            {
                return LazyDefaultRecorder.Value;
            }
        }

        /// <summary>
        /// Begin a tracing segment. A new tracing segment will be created and started.
        /// </summary>
        /// <param name="name">The name of the segment.</param>
        /// <param name="traceId">Trace id of the segment.</param>
        /// <param name="parentId">Unique id of the upstream remote segment or subsegment where the downstream call originated from.</param>
        /// <param name="sampleDecision">Sample decision for the segment from upstream service.</param>
        /// <exception cref="ArgumentNullException">The argument has a null value.</exception>
        public override void BeginSegment(string name, string traceId=null, string parentId = null, SampleDecision sampleDecision = SampleDecision.Sampled)
        {
            Segment newSegment = new Segment(name, traceId, parentId);
            if (!IsTracingDisabled())
            {
                newSegment.SetStartTimeToNow(); //sets current timestamp
                PopulateNewSegmentAttributes(newSegment);
            }

            newSegment.Sampled = sampleDecision;
            TraceContext.SetEntity(newSegment);
        }

        /// <summary>
        /// End a tracing segment. If all operations of the segments are finished, the segment will be emitted.
        /// </summary>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public override void EndSegment()
        {
            try
            {
                // If the request is not sampled, a segment will still be available in TraceContext.
                // Need to clean up the segment, but do not emit it.
                Segment segment = (Segment)TraceContext.GetEntity();

                if (!IsTracingDisabled())
                {
                    segment.SetEndTimeToNow(); //sets end time to current time
                    ProcessEndSegment(segment);
                }

                TraceContext.ClearEntity();
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to end segment because cannot get the segment from trace context.");
            }
            catch (InvalidCastException e)
            {
                HandleEntityNotAvailableException(new EntityNotAvailableException("Failed to cast the entity to Segment.", e), "Failed to cast the entity to Segment.");
            }
        }

        /// <summary>
        /// Begin a tracing subsegment. A new segment will be created and added as a subsegment to previous segment.
        /// </summary>
        /// <param name="name">Name of the operation.</param>
        /// <exception cref="ArgumentNullException">The argument has a null value.</exception>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public override void BeginSubsegment(string name)
        {
            try
            {
                if (IsTracingDisabled())
                {
                    _logger.DebugFormat("X-Ray tracing is disabled, do not start subsegment");
                    return;
                }

                // If the request is not sampled, a segment will still be available in TraceContext to
                // stores the information of the trace. The trace information will still propagated to 
                // downstream service, in case downstream may overwrite the sample decision.
                Entity parentEntity = TraceContext.GetEntity();

                // If the segment is not sampled, do nothing and exit.
                if (parentEntity.Sampled != SampleDecision.Sampled)
                {
                    _logger.DebugFormat("Do not start subsegment because the segment doesn't get sampled. ({0})", name);
                    return;
                }

                Subsegment subsegment = new Subsegment(name);
                parentEntity.AddSubsegment(subsegment);
                subsegment.Sampled = parentEntity.Sampled;
                subsegment.SetStartTimeToNow();
                TraceContext.SetEntity(subsegment);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to start subsegment because the parent segment is not available.");
            }
        }

        /// <summary>
        /// End a subsegment.
        /// </summary>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public override void EndSubsegment()
        {
            try
            {
                if (IsTracingDisabled())
                {
                    _logger.DebugFormat("X-Ray tracing is disabled, do not end subsegment");
                    return;
                }

                ProcessEndSubsegment();
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to end subsegment because subsegment is not available in trace context.");
            }
            catch (InvalidCastException e)
            {
                HandleEntityNotAvailableException(new EntityNotAvailableException("Failed to cast the entity to Subsegment.", e), "Failed to cast the entity to Subsegment.");
            }
        }

        /// <summary>
        ///  Checks whether Tracing is enabled or disabled.
        /// </summary>
        /// <returns> Returns true if Tracing is disabled else false.</returns>
        public override bool IsTracingDisabled()
        {
            return AppSettings.IsXRayTracingDisabled;
        }
    }
}
