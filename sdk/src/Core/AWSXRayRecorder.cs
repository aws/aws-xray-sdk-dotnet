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
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Strategies;

namespace Amazon.XRay.Recorder.Core
{
    /// <summary>
    /// A collection of methods used to record tracing information for AWS X-Ray.
    /// </summary>
    /// <see cref="Amazon.XRay.Recorder.Core.IAWSXRayRecorder" />
    public class AWSXRayRecorder : AWSXRayRecorderImpl
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorder));

        public static bool IsCustomRecorder;

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
            SamplingStrategy = new DefaultSamplingStrategy(AppSettings.SamplingRuleManifest);
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

            private set
            {
                LazyDefaultRecorder = new Lazy<AWSXRayRecorder>(() => value);
            }
        }

        /// <summary>
        /// Sets provided instance of the <see cref="AWSXRayRecorder" /> to AWSXRayRecorder.Instance
        /// </summary>
        /// <param name="recorder">Instance of <see cref="AWSXRayRecorder"/>.</param>
        public static void InitializeInstance(AWSXRayRecorder recorder)
        {
            if (recorder != null)
            {
                _logger.DebugFormat("Using custom X-Ray recorder.");
                Instance = recorder;
                IsCustomRecorder = true;
            }
            else
            {
                _logger.DebugFormat("Provided X-Ray recorder is null, using defaul recorder.");
            }
        }

        /// <summary>
        /// Begin a tracing subsegment. A new segment will be created and added as a subsegment to previous segment.
        /// </summary>
        /// <param name="name">Name of the operation.</param>
        /// <param name="timestamp">Sets the start time for the subsegment.</param>
        /// <exception cref="ArgumentNullException">The argument has a null value.</exception>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public override void BeginSubsegment(string name, DateTime? timestamp = null)
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
                if (timestamp == null)
                {
                    subsegment.SetStartTimeToNow();
                }
                else
                {
                    subsegment.SetStartTime(timestamp.Value);
                }
                
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
        /// <param name="timestamp">Sets the end time of the subsegment</param>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public override void EndSubsegment(DateTime? timestamp = null)
        {
            try
            {
                if (IsTracingDisabled())
                {
                    _logger.DebugFormat("X-Ray tracing is disabled, do not end subsegment");
                    return;
                }

                ProcessEndSubsegment(timestamp);
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
