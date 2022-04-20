//-----------------------------------------------------------------------------
// <copyright file="AWSXRayRecorder.netcore.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Sampling.Local;
using Microsoft.Extensions.Configuration;
namespace Amazon.XRay.Recorder.Core
{
    /// <summary>
    /// A collection of methods used to record tracing information for AWS X-Ray.
    /// </summary>
    /// <seealso cref="Amazon.XRay.Recorder.Core.IAWSXRayRecorder" />
    public class AWSXRayRecorder : AWSXRayRecorderImpl
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorder));
        static AWSXRayRecorder _instance = new AWSXRayRecorderBuilder().Build();
        public const String LambdaTaskRootKey = "LAMBDA_TASK_ROOT";
        public const String LambdaTraceHeaderKey = "_X_AMZN_TRACE_ID";

        private static String _lambdaVariables;

        private XRayOptions _xRayOptions = new XRayOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class.
        /// with default configuration.
        /// </summary>
        public AWSXRayRecorder() : this(new UdpSegmentEmitter())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class with <see cref="XRayOptions"/>
        /// </summary>
        /// <param name="options">Instance of <see cref="XRayOptions"/>.</param>
        public AWSXRayRecorder(XRayOptions options) : this(new UdpSegmentEmitter(), options)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class
        /// with given instance of <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">Instance of <see cref="IConfiguration"/>.</param>
        [CLSCompliant(false)]
        public static void InitializeInstance(IConfiguration configuration)
        {
            XRayOptions xRayOptions = XRayConfiguration.GetXRayOptions(configuration);

            var recorderBuilder = GetBuilder(xRayOptions);

            Instance = recorderBuilder.Build(xRayOptions);
        }

        /// <summary>
        /// Initializes provided instance of the <see cref="AWSXRayRecorder" /> class with 
        /// the instance of <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">Instance of <see cref="IConfiguration"/>.</param>
        /// <param name="recorder">Instance of <see cref="AWSXRayRecorder"/>.</param>
        [CLSCompliant(false)]
        public static void InitializeInstance(IConfiguration configuration = null, AWSXRayRecorder recorder = null)
        {
            XRayOptions xRayOptions = XRayConfiguration.GetXRayOptions(configuration);
            var recorderBuilder = GetBuilder(xRayOptions);

            if (recorder != null)
            {
                _logger.DebugFormat("Using custom X-Ray recorder.");
                recorder.XRayOptions = xRayOptions;
                recorder = recorderBuilder.Build(recorder);
            }
            else
            {
                _logger.DebugFormat("Using default X-Ray recorder.");
                recorder = recorderBuilder.Build(xRayOptions);
            }

            Instance = recorder;
        }

        private static AWSXRayRecorderBuilder GetBuilder(XRayOptions xRayOptions)
        {
            var recorderBuilder = new AWSXRayRecorderBuilder().WithPluginsFromConfig(xRayOptions).WithContextMissingStrategyFromConfig(xRayOptions);
            return recorderBuilder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class
        /// with given instance of <see cref="ISegmentEmitter" />.
        /// </summary>
        /// <param name="emitter">Segment emitter</param>
        internal AWSXRayRecorder(ISegmentEmitter emitter) : base(emitter)
        {
            PopulateContexts();

            if (IsLambda())
            {
                SamplingStrategy = new LocalizedSamplingStrategy();
            }
            else
            {
                SamplingStrategy = new DefaultSamplingStrategy();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayRecorder" /> class
        /// with given instance of <see cref="ISegmentEmitter" /> and instance of <see cref="XRayOptions"/>.
        /// </summary>
        /// <param name="emitter">Instance of <see cref="ISegmentEmitter"/>.</param>
        /// <param name="options">Instance of <see cref="XRayOptions"/>.</param>
        internal AWSXRayRecorder(ISegmentEmitter emitter, XRayOptions options) : base(emitter)
        {
            XRayOptions = options;
            PopulateContexts();

            if (IsLambda())
            {
                SamplingStrategy = new LocalizedSamplingStrategy(options.SamplingRuleManifest);
            }
            else
            {
                SamplingStrategy = new DefaultSamplingStrategy(options.SamplingRuleManifest);
            }
        }
        /// <summary>
        /// Gets the singleton instance of <see cref="AWSXRayRecorder"/> with default configuration.
        /// </summary>
        /// <returns>An instance of <see cref="AWSXRayRecorder"/> class.</returns>
        public static AWSXRayRecorder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AWSXRayRecorderBuilder().Build();
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Instance of <see cref="XRayOptions"/> class.
        /// </summary>
        public XRayOptions XRayOptions { get => _xRayOptions; set => _xRayOptions = value; }

        /// <summary>
        /// Begin a tracing subsegment. A new segment will be created and added as a subsegment to previous segment/subsegment.
        /// </summary>
        /// <param name="name">Name of the operation</param>
        /// <param name="timestamp">Sets the start time of the subsegment</param>
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

                if (IsLambda())
                {
                    ProcessSubsegmentInLambdaContext(name, timestamp);
                }
                else
                {
                    AddSubsegment(new Subsegment(name), timestamp);
                }
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to start subsegment because the parent segment is not available.");
            }
        }

        /// <summary>
        /// Begin a tracing subsegment. A new subsegment will be created and added as a subsegment to previous facade segment or subsegment.
        /// </summary>
        private void ProcessSubsegmentInLambdaContext(string name, DateTime? timestamp = null)
        {
            if (!TraceContext.IsEntityPresent()) // No facade segment available and first subsegment of a subsegment branch needs to be added
            {
                AddFacadeSegment(name);
                AddSubsegmentInLambdaContext(name, timestamp);
            }
            else // Facade / Subsegment already present
            {
                var entity = TraceContext.GetEntity(); // can be Facade segment or Subsegment
                var environmentRootTraceId = TraceHeader.FromString(AWSXRayRecorder.GetTraceVariablesFromEnvironment()).RootTraceId;

                if ((null != environmentRootTraceId) && !environmentRootTraceId.Equals(entity.RootSegment.TraceId)) // If true, customer has leaked subsegments across invocation
                {
                    TraceContext.ClearEntity(); // reset TraceContext
                    BeginSubsegment(name, timestamp); // This adds Facade segment with updated environment variables
                }
                else
                {
                    AddSubsegmentInLambdaContext(name, timestamp);
                }
            }
        }

        /// <summary>
        /// Begin a Facade Segment.
        /// </summary>
        internal void AddFacadeSegment(String name = null)
        {
            _lambdaVariables = AWSXRayRecorder.GetTraceVariablesFromEnvironment();
            _logger.DebugFormat("Lambda Environment detected. Lambda variables: {0}", _lambdaVariables);

            if (!TraceHeader.TryParseAll(_lambdaVariables, out TraceHeader traceHeader))
            {
                if (name != null)
                {
                    _logger.DebugFormat("Lambda variables : {0} for X-Ray trace header environment variable under key : {1} are missing/not valid trace id, parent id or sampling decision, discarding subsegment : {2}", _lambdaVariables, LambdaTraceHeaderKey, name);
                }
                else
                {
                    _logger.DebugFormat("Lambda variables : {0} for X-Ray trace header environment variable under key : {1} are missing/not valid trace id, parent id or sampling decision, discarding subsegment", _lambdaVariables, LambdaTraceHeaderKey);
                }

                traceHeader = new TraceHeader();
                traceHeader.RootTraceId = TraceId.NewId();
                traceHeader.ParentId = null;
                traceHeader.Sampled = SampleDecision.NotSampled;
            }

            Segment newSegment = new FacadeSegment("Facade", traceHeader.RootTraceId, traceHeader.ParentId);
            newSegment.Sampled = traceHeader.Sampled;
            TraceContext.SetEntity(newSegment);
        }

        private void AddSubsegmentInLambdaContext(string name, DateTime? timestamp = null)
        {
            // If the request is not sampled, the passed subsegment will still be available in TraceContext to
            // stores the information of the trace. The trace information will still propagated to 
            // downstream service, in case downstream may overwrite the sample decision.
            Entity parentEntity = TraceContext.GetEntity();
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

        private void AddSubsegment(Subsegment subsegment, DateTime? timestamp = null)
        {
            // If the request is not sampled, a segment will still be available in TraceContext to
            // stores the information of the trace. The trace information will still propagated to 
            // downstream service, in case downstream may overwrite the sample decision.
            Entity parentEntity = TraceContext.GetEntity();

            // If the segment is not sampled, do nothing and exit.
            if (parentEntity.Sampled != SampleDecision.Sampled)
            {
                _logger.DebugFormat("Do not start subsegment because the segment doesn't get sampled. ({0})", subsegment.Name);
                return;
            }

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

        /// <summary>
        /// End a subsegment.
        /// </summary>
        /// <param name="timestamp">Sets the end time for the subsegment</param>
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

                if (IsLambda())
                {
                    ProcessEndSubsegmentInLambdaContext(timestamp);
                }
                else
                {
                    ProcessEndSubsegment(timestamp);
                }
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

        private void ProcessEndSubsegmentInLambdaContext(DateTime? timestamp = null)
        {
            var subsegment = PrepEndSubsegmentInLambdaContext();

            if (timestamp == null)
            {
                subsegment.SetEndTimeToNow();
            }
            else
            {
                subsegment.SetEndTime(timestamp.Value);
            }

            // Check emittable
            if (subsegment.IsEmittable())
            {
                // Emit
                Emitter.Send(subsegment.RootSegment);
            }
            else if (StreamingStrategy.ShouldStream(subsegment))
            {
                StreamingStrategy.Stream(subsegment.RootSegment, Emitter);
            }

            if (TraceContext.IsEntityPresent() && TraceContext.GetEntity().GetType() == typeof(FacadeSegment)) //implies FacadeSegment in the Trace Context
            {
                EndFacadeSegment();
                return;
            }
        }

        private Subsegment PrepEndSubsegmentInLambdaContext()
        {
            // If the request is not sampled, a subsegment will still be available in TraceContext.
            //This behavor is specific to AWS Lambda environment
            Entity entity = TraceContext.GetEntity();
            Subsegment subsegment = (Subsegment)entity;

            subsegment.IsInProgress = false;

            // Restore parent segment to trace context
            if (subsegment.Parent != null)
            {
                TraceContext.SetEntity(subsegment.Parent);
            }

            // Drop ref count
            subsegment.Release();

            return subsegment;
        }

        private void EndFacadeSegment()
        {
            try
            {
                // If the request is not sampled, a segment will still be available in TraceContext.
                // Need to clean up the segment, but do not emit it.
                FacadeSegment facadeSegment = (FacadeSegment)TraceContext.GetEntity();

                if (!IsTracingDisabled())
                {
                    PrepEndSegment(facadeSegment);
                    if (facadeSegment.Sampled == SampleDecision.Sampled && facadeSegment.RootSegment != null && facadeSegment.RootSegment.Size >= 0)
                    {
                        StreamingStrategy.Stream(facadeSegment, Emitter); //Facade segment is not emitted, all its subsegments, if emmittable, are emitted
                    }
                }

                TraceContext.ClearEntity();
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to end facade segment because cannot get the segment from trace context.");
            }
            catch (InvalidCastException e)
            {
                HandleEntityNotAvailableException(new EntityNotAvailableException("Failed to cast the entity to Facade segment.", e), "Failed to cast the entity to Facade Segment.");
            }
        }

        /// <summary>
        /// Checks whether current execution is in AWS Lambda.
        /// </summary>
        /// <returns>Returns true if current execution is in AWS Lambda.</returns>
        public static Boolean IsLambda()
        {
            var lambdaTaskRootKey = Environment.GetEnvironmentVariable(LambdaTaskRootKey);

            if (!Object.Equals(lambdaTaskRootKey, null))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns value set for environment variable "_X_AMZN_TRACE_ID"
        /// </summary>
        private static String GetTraceVariablesFromEnvironment()
        {
            var lambdaTraceHeader = Environment.GetEnvironmentVariable(LambdaTraceHeaderKey);
            return lambdaTraceHeader;
        }

        /// <summary>
        /// Checks whether Tracing is enabled or disabled.
        /// </summary>
        /// <returns> Returns true if Tracing is disabled else false.</returns>
        public override bool IsTracingDisabled()
        {
            return XRayOptions.IsXRayTracingDisabled;
        }

        /// <summary>
        ///  Configures Logger to <see cref="Amazon.LoggingOptions"/>.
        /// </summary>
        /// <param name="loggingOptions">Enum of <see cref="Amazon.LoggingOptions"/>.</param>
        public static void RegisterLogger(Amazon.LoggingOptions loggingOptions)
        {
            AWSConfigs.LoggingConfig.LogTo = loggingOptions;
        }
    }
}
