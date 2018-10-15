//-----------------------------------------------------------------------------
// <copyright file="AWSXRayRecorderImpl.cs" company="Amazon.com">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Exceptions;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;

namespace Amazon.XRay.Recorder.Core
{
    /// <summary>
    /// This class provides utilities to build an instance of <see cref="AWSXRayRecorder"/> with different configurations.
    /// </summary>
    public abstract class AWSXRayRecorderImpl : IAWSXRayRecorder
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorderImpl));
#if NET45
        private static Lazy<AWSXRayRecorder> _lazyDefaultRecorder = new Lazy<AWSXRayRecorder>(() => AWSXRayRecorderBuilder.GetDefaultBuilder().Build());
        protected static Lazy<AWSXRayRecorder> LazyDefaultRecorder
        {
            get
            {
                return _lazyDefaultRecorder;
            }
            set
            {
                _lazyDefaultRecorder = value;
            }
        }
#endif
        /// <summary>
        /// Instance of <see cref="ITraceContext"/>, used to store segment/subsegment.
        /// </summary>
        public ITraceContext TraceContext = DefaultTraceContext.GetTraceContext();

        /// <summary>
        /// The environment variable that setting context missing strategy.
        /// </summary>
        public const string EnvironmentVariableContextMissingStrategy = "AWS_XRAY_CONTEXT_MISSING";

        protected const long MaxSubsegmentSize = 100;

        private ISegmentEmitter _emitter;
        private bool disposed;
        protected ContextMissingStrategy cntxtMissingStrategy = ContextMissingStrategy.RUNTIME_ERROR;
        private Dictionary<string, object> serviceContext = new Dictionary<string, object>();

        protected AWSXRayRecorderImpl(ISegmentEmitter emitter)
        {
            this._emitter = emitter;
        }

        /// <summary>
        /// Gets or sets the origin of the service.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Gets or sets the sampling strategy.
        /// </summary>
        public ISamplingStrategy SamplingStrategy { get; set; }

        /// <summary>
        /// Gets or sets the context missing strategy.
        /// </summary>
        public ContextMissingStrategy ContextMissingStrategy
        {
            get
            {
                return cntxtMissingStrategy;
            }

            set
            {
                cntxtMissingStrategy = value;
                _logger.DebugFormat(string.Format("Context missing mode : {0}", cntxtMissingStrategy));
                string modeFromEnvironmentVariable = Environment.GetEnvironmentVariable(EnvironmentVariableContextMissingStrategy);
                if (string.IsNullOrEmpty(modeFromEnvironmentVariable))
                {
                    _logger.DebugFormat(string.Format("{0} environment variable is not set. Do not override context missing mode.", EnvironmentVariableContextMissingStrategy));
                }
                else if (modeFromEnvironmentVariable.Equals(ContextMissingStrategy.LOG_ERROR.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.DebugFormat(string.Format("{0} environment variable is set to {1}. Override local value.", EnvironmentVariableContextMissingStrategy, modeFromEnvironmentVariable));
                    cntxtMissingStrategy = ContextMissingStrategy.LOG_ERROR;
                }
                else if (modeFromEnvironmentVariable.Equals(ContextMissingStrategy.RUNTIME_ERROR.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.DebugFormat(string.Format("{0} environment variable is set to {1}. Override local value.", EnvironmentVariableContextMissingStrategy, modeFromEnvironmentVariable));
                    cntxtMissingStrategy = ContextMissingStrategy.RUNTIME_ERROR;
                }
            }
        }

        /// <summary>
        /// Gets the runtime context which is generated by plugins.
        /// </summary>
        public IDictionary<string, object> RuntimeContext { get; protected set; }

        public ISegmentEmitter Emitter { get => _emitter; set => _emitter = value; }

        protected bool Disposed { get => disposed; set => disposed = value; }

        protected Dictionary<string, object> ServiceContext { get => serviceContext; set => serviceContext = value; }

        /// <summary>
        /// Begin a tracing segment. A new tracing segment will be created and started.
        /// </summary>
        /// <param name="name">The name of the segment</param>
        /// <param name="traceId">Trace id of the segment</param>
        /// <param name="parentId">Unique id of the upstream remote segment or subsegment where the downstream call originated from.</param>
        /// <param name="samplingResponse">Instance  of <see cref="SamplingResponse"/>, contains sampling decision for the segment from upstream service. If not passed, sampling decision is made based on <see cref="SamplingStrategy"/> set with the recorder instance.</param>
        /// <param name="timestamp">If not null, sets the start time for the segment else current time is set.</param>
        /// <exception cref="ArgumentNullException">The argument has a null value.</exception>
        public void BeginSegment(string name, string traceId = null, string parentId = null, SamplingResponse samplingResponse = null, DateTime? timestamp = null)
        {
#if !NET45
            if (AWSXRayRecorder.IsLambda())
            {
                throw new UnsupportedOperationException("Cannot override Facade Segment. New segment not created.");
            }
#endif
            Segment newSegment = new Segment(name, traceId, parentId);

            if (samplingResponse == null)
            {
                SamplingInput samplingInput = new SamplingInput();
                samplingResponse = SamplingStrategy.ShouldTrace(samplingInput);
            }

            if (!IsTracingDisabled())
            {
                if (timestamp == null)
                {
                    newSegment.SetStartTimeToNow();
                }
                else
                {

                    newSegment.SetStartTime(timestamp.Value);
                }

                PopulateNewSegmentAttributes(newSegment, samplingResponse);
            }

            newSegment.Sampled = samplingResponse.SampleDecision;

            TraceContext.SetEntity(newSegment);
        }


        /// <summary>
        /// End tracing of a given segment.
        /// </summary>
        /// <param name="timestamp">If not null, set as endtime for the current segment.</param>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void EndSegment(DateTime? timestamp = null)
        {
#if !NET45
            if (AWSXRayRecorder.IsLambda())
            {
                throw new UnsupportedOperationException("Cannot override Facade Segment. New segment not created.");
            }
#endif
            try
            {
                // If the request is not sampled, a segment will still be available in TraceContext.
                // Need to clean up the segment, but do not emit it.
                Segment segment = (Segment)TraceContext.GetEntity();

                if (!IsTracingDisabled())
                {
                    if (timestamp == null)
                    {
                        segment.SetEndTimeToNow();
                    }
                    else
                    {
                        segment.SetEndTime(timestamp.Value); // sets custom endtime
                    }

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
        public abstract void BeginSubsegment(string name);

        /// <summary>
        /// End a subsegment.
        /// </summary>
        public abstract void EndSubsegment();

        /// <summary>
        /// Checks whether Tracing is enabled or disabled.
        /// </summary>
        /// <returns> Returns true if Tracing is disabled else false.</returns>
        public abstract Boolean IsTracingDisabled();

        /// <summary>
        /// Adds the specified key and value as annotation to current segment.
        /// The type of value is restricted. Only <see cref="string" />, <see cref="int" />, <see cref="long" />,
        /// <see cref="double" /> and <see cref="bool" /> are supported.
        /// </summary>
        /// <param name="key">The key of the annotation to add.</param>
        /// <param name="value">The value of the annotation to add.</param>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void AddAnnotation(string key, object value)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add annotation.");
                return;
            }

            try
            {
                TraceContext.GetEntity().AddAnnotation(key, value);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add annotation because subsegment is not available in trace context.");
            }
        }

        /// <summary>
        /// Set namespace to current segment.
        /// </summary>
        /// <param name="value">The value of the namespace.</param>
        /// <exception cref="System.ArgumentException">Value cannot be null or empty.</exception>
        public void SetNamespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.", "value");
            }

            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not set namespace.");
                return;
            }

            try
            {
                var subsegment = TraceContext.GetEntity() as Subsegment;

                if (subsegment == null)
                {
                    _logger.DebugFormat("Failed to cast the entity from TraceContext to Subsegment. SetNamespace is only available to Subsegment.");
                    return;
                }

                subsegment.Namespace = value;
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to set namespace because of subsegment is not available.");
            }
        }

        /// <summary>
        /// Populates runtime and service contexts for the segment.
        /// </summary>
        /// <param name="newSegment">Instance of <see cref="Segment"/>.</param>
        protected void PopulateNewSegmentAttributes(Segment newSegment)
        {
            if (RuntimeContext != null)
            {
                foreach (var keyValuePair in RuntimeContext)
                {
                    newSegment.Aws[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            if (Origin != null)
            {
                newSegment.Origin = Origin;
            }

            foreach (var keyValuePair in ServiceContext)
            {
                newSegment.Service[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        /// <summary>
        /// Populates runtime and service contexts for the segment.
        /// </summary>
        /// <param name="newSegment">Instance of <see cref="Segment"/>.</param>
        protected void PopulateNewSegmentAttributes(Segment newSegment, SamplingResponse sampleResponse)
        {
            if (RuntimeContext != null)
            {
                foreach (var keyValuePair in RuntimeContext)
                {
                    newSegment.Aws[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            AddRuleName(newSegment, sampleResponse);

            if (Origin != null)
            {
                newSegment.Origin = Origin;
            }

            foreach (var keyValuePair in ServiceContext)
            {
                newSegment.Service[keyValuePair.Key] = keyValuePair.Value;
            }
        }

        /// <summary>
        /// If non null, adds given rulename to the segment..
        /// </summary>
        private void AddRuleName(Segment newSegment, SamplingResponse sampleResponse)
        {
            var ruleName = sampleResponse.RuleName;
            string ruleNameKey = "sampling_rule_name";
            if (string.IsNullOrEmpty(ruleName))
            {
                return;
            }
            IDictionary<string, string> xrayContext;
            if (newSegment.Aws.TryGetValue("xray", out object value))
            {
                IDictionary<string, string> tempXrayContext = (Dictionary<string, string>) value;
                xrayContext = new Dictionary<string, string>(tempXrayContext); // deep copy for thread safety
                xrayContext[ruleNameKey] = ruleName;
            }
            else
            {
                xrayContext = new Dictionary<string, string>();
                xrayContext[ruleNameKey] = ruleName;
            }

            newSegment.Aws["xray"] = xrayContext;
        }

        /// <summary>
        /// Adds the specified key and value as http information to current segment.
        /// </summary>
        /// <param name="key">The key of the http information to add.</param>
        /// <param name="value">The value of the http information to add.</param>
        /// <exception cref="ArgumentException">Key is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void AddHttpInformation(string key, object value)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add http information.");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", "key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            try
            {
                TraceContext.GetEntity().Http[key] = value;
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add http because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Mark the current segment as fault.
        /// </summary>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void MarkFault()
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not mark fault.");
                return;
            }

            try
            {
                Entity entity = TraceContext.GetEntity();
                entity.HasFault = true;
                entity.HasError = false;
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to mark fault because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Mark the current segment as error.
        /// </summary>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void MarkError()
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not mark error.");
                return;
            }

            try
            {
                Entity entity = TraceContext.GetEntity();
                entity.HasError = true;
                entity.HasFault = false;
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to mark error because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Add the exception to current segment and also mark current segment as fault.
        /// </summary>
        /// <param name="ex">The exception to be added.</param>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void AddException(Exception ex)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add exception.");
                return;
            }

            try
            {
                TraceContext.GetEntity().AddException(ex);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add exception because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Mark the current segment as being throttled. And Error will also be marked for current segment.
        /// </summary>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void MarkThrottle()
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not mark throttle.");
                return;
            }

            try
            {
                TraceContext.GetEntity().IsThrottled = true;
                MarkError();
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to mark throttle because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Add a precursor id.
        /// </summary>
        /// <param name="precursorId">The precursor id to be added.</param>
        public void AddPrecursorId(string precursorId)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add precursorId.");
                return;
            }

            try
            {
                var subsegment = TraceContext.GetEntity() as Subsegment;
                if (subsegment == null)
                {
                    _logger.DebugFormat("Can't cast the Entity from TraceContext to Subsegment. The AddPrecursorId is only available for subsegment");
                    return;
                }

                subsegment.AddPrecursorId(precursorId);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add precursor id because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Add the specified key and value as SQL information to current segment.
        /// </summary>
        /// <param name="key">The key of the SQL information.</param>
        /// <param name="value">The value of the http information.</param>
        /// <exception cref="ArgumentException">Value or key is null or empty.</exception>
        /// <exception cref="EntityNotAvailableException">Entity is not available in trace context.</exception>
        public void AddSqlInformation(string key, string value)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add sql information.");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", "key");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty", "value");
            }

            try
            {
                TraceContext.GetEntity().Sql[key] = value;
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add sql information because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Adds the specified key and value to metadata under default namespace.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddMetadata(string key, object value)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add metadata.");
                return;
            }

            try
            {
                TraceContext.GetEntity().AddMetadata(key, value);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add metadata because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Adds the specified key and value to metadata with given namespace.
        /// </summary>
        /// <param name="nameSpace">The namespace.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddMetadata(string nameSpace, string key, object value)
        {
            if (IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not add metadata.");
                return;
            }

            try
            {
                TraceContext.GetEntity().AddMetadata(nameSpace, key, value);
            }
            catch (EntityNotAvailableException e)
            {
                HandleEntityNotAvailableException(e, "Failed to add metadata because segment is not available in trace context.");
            }
        }

        /// <summary>
        /// Sets the daemon address for <see cref="Emitter"/> and <see cref="DefaultSamplingStrategy"/> if set.
        /// A notation of '127.0.0.1:2000' or 'tcp:127.0.0.1:2000 udp:127.0.0.2:2001' or 
        ///'udp:127.0.0.1:2000 tcp:127.0.0.2:2001'
        /// are acceptable.The former one means UDP and TCP are running at
        /// the same address.
        /// If environment variable is set to specific daemon address, the call to this method
        /// will be ignored.
        /// </summary>
        /// <param name="daemonAddress">The daemon address.</param>
        public void SetDaemonAddress(string daemonAddress)
        {
            if (Emitter != null)
            {
                Emitter.SetDaemonAddress(daemonAddress);
            }

            if (SamplingStrategy != null && SamplingStrategy.GetType().Equals(typeof(DefaultSamplingStrategy)))
            {
                DefaultSamplingStrategy defaultSampler = (DefaultSamplingStrategy)SamplingStrategy;
                defaultSampler.LoadDaemonConfig(DaemonConfig.GetEndPoint(daemonAddress));
            }
        }

        /// <summary>
        /// Configures recorder instance with <see cref="ITraceContext"/>.
        /// </summary>
        /// <param name="traceContext">Instance of <see cref="ITraceContext"/></param>
        public void SetTraceContext(ITraceContext traceContext)
        {
            if (traceContext != null)
            {
                TraceContext = traceContext;
            }
        }

        /// <summary>
        /// Free resources within the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Free resources within the object.
        /// </summary>
        /// <param name="disposing">To dispose or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                if (Emitter != null)
                {
                    Emitter.Dispose();
                }

                Disposed = true;
            }
        }

        /// <summary>
        /// Checks whether subsegments of the current instance of  <see cref="Entity"/> should be streamed.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/></param>
        /// <returns>True if the subsegments are streamable.</returns>
        protected static bool ShouldStreamSubsegments(Entity entity)
        {
            return entity.Sampled == SampleDecision.Sampled && entity.RootSegment != null && entity.RootSegment.Size >= MaxSubsegmentSize;
        }

        /// <summary>
        /// Streams subsegments of instance of <see cref="Entity"/>.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/>.</param>
        protected void StreamSubsegments(Entity entity)
        {
            lock (entity.Subsegments)
            {
                foreach (var next in entity.Subsegments)
                {
                    StreamSubsegments(next);
                }

                entity.Subsegments.RemoveAll(x => x.HasStreamed);
            }

            if (entity is Segment || entity.IsInProgress || entity.Reference > 0 || entity.IsSubsegmentsAdded)
            {
                return;
            }

            Subsegment subsegment = entity as Subsegment;
            subsegment.TraceId = entity.RootSegment.TraceId;
            subsegment.Type = "subsegment";
            subsegment.ParentId = subsegment.Parent.Id;
            Emitter.Send(subsegment);
            subsegment.RootSegment.DecrementSize();
            subsegment.HasStreamed = true;
        }

        /// <summary>
        /// Returns subsegments.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/></param>
        /// <returns>Subsegments of instance of <see cref="Entity"/>.</returns>
        protected Subsegment[] GetSubsegmentsToStream(Entity entity)
        {
            Subsegment[] copy;
            lock (entity.Subsegments)
            {
                copy = new Subsegment[entity.Subsegments.Count];
                entity.Subsegments.CopyTo(copy);
            }

            return copy;
        }

        /// <summary>
        /// Populates runtime and service contexts.
        /// </summary>
        protected void PopulateContexts()
        {
            RuntimeContext = new Dictionary<string, object>();

            // Prepare XRay section for runtime context
            var xrayContext = new Dictionary<string, string>();

#if NET45
            xrayContext["sdk"] = "X-Ray for .NET";
#else
            xrayContext["sdk"] = "X-Ray for .NET Core";
#endif
            string currentAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(currentAssemblyLocation))
            {
                xrayContext["sdk_version"] = FileVersionInfo.GetVersionInfo(currentAssemblyLocation).ProductVersion;
            }
            else
            {
                xrayContext["sdk_version"] = "Unknown";
            }

            RuntimeContext["xray"] = xrayContext;
#if NET45
            ServiceContext["runtime"] = ".NET Framework";
#else
            ServiceContext["runtime"] = ".NET Core Framework";
#endif
            ServiceContext["runtime_version"] = Environment.Version.ToString();
        }

        /// <summary>
        /// If sampled and is emittable sends segments using emitter else checks for subsegments to stream.
        /// </summary>
        /// <param name="segment"></param>
        protected void ProcessEndSegment(Segment segment)
        {
            PrepEndSegment(segment);

            if (segment.Sampled == SampleDecision.Sampled && segment.IsEmittable())
            {
                Emitter.Send(segment);
            }
            else if (ShouldStreamSubsegments(segment))
            {
                StreamSubsegments(segment);
            }
        }

        /// <summary>
        /// Sets segment IsInProgress to false and releases the segment.
        /// </summary>
        /// <param name="segment">Instance of <see cref="Segment"/>.</param>
        protected void PrepEndSegment(Segment segment)
        {
            segment.IsInProgress = false;
            segment.Release();
        }

        /// <summary>
        /// Sends root segment of the current subsegment.
        /// </summary>
        protected void ProcessEndSubsegment()
        {
            var subsegment = PrepEndSubsegment();

            if (subsegment == null)
            {
                return;
            }
            // Check emittable
            if (subsegment.IsEmittable())
            {
                // Emit
                Emitter.Send(subsegment.RootSegment);
            }
            else if (ShouldStreamSubsegments(subsegment))
            {
                StreamSubsegments(subsegment.RootSegment);
            }
        }

        private Subsegment PrepEndSubsegment()
        {
            // If the request is not sampled, a segment will still be available in TraceContext.
            Entity entity = TraceContext.GetEntity();

            // If the segment is not sampled, a subsegment is not created. Do nothing and exit.
            if (entity.Sampled != SampleDecision.Sampled)
            {
                return null;
            }

            Subsegment subsegment = (Subsegment)entity;

            // Set end time
            subsegment.SetEndTimeToNow();
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
        /// <summary>
        /// If entity is not available in the <see cref="TraceContext"/>, exception is thrown.
        /// </summary>
        /// <param name="e">Instance of <see cref="EntityNotAvailableException"/>.</param>
        /// <param name="message">Stirng message.</param>
        protected void HandleEntityNotAvailableException(EntityNotAvailableException e, string message)
        {
            _logger.Error(e, message);

            if (ContextMissingStrategy == ContextMissingStrategy.LOG_ERROR)
            {
                _logger.DebugFormat("The ContextMissingStrategy is set to be LOG_ERROR. EntityNotAvailableException exception is suppressed.");
            }
            else
            {
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        /// <summary>
        /// Trace a given function with return value. A subsegment will be created for this method.
        /// Any exception thrown by the method will be captured.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
        /// <param name="name">The name of the trace subsegment for the method.</param>
        /// <param name="method">The method to be traced.</param>
        /// <returns>The return value of the given method.</returns>
        public TResult TraceMethod<TResult>(string name, Func<TResult> method)
        {
            BeginSubsegment(name);

            try
            {
                return method();
            }
            catch (Exception e)
            {
                AddException(e);
                throw;
            }

            finally
            {
                EndSubsegment();
            }
        }

        /// <summary>
        /// Trace a given method returns void.  A subsegment will be created for this method.
        /// Any exception thrown by the method will be captured.
        /// </summary>
        /// <param name="name">The name of the trace subsegment for the method.</param>
        /// <param name="method">The method to be traced.</param>
        public void TraceMethod(string name, Action method)
        {
            BeginSubsegment(name);

            try
            {
                method();
            }
            catch (Exception e)
            {
                AddException(e);
                throw;
            }

            finally
            {
                EndSubsegment();
            }
        }

        /// <summary>
        /// Trace a given asynchronous function with return value. A subsegment will be created for this method.
        /// Any exception thrown by the method will be captured.
        /// </summary>
        /// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates</typeparam>
        /// <param name="name">The name of the trace subsegment for the method</param>
        /// <param name="method">The method to be traced</param>
        /// <returns>The return value of the given method</returns>
        public async Task<TResult> TraceMethodAsync<TResult>(string name, Func<Task<TResult>> method)
        {
            BeginSubsegment(name);

            try
            {
                return await method();
            }
            catch (Exception e)
            {
                AddException(e);
                throw;
            }

            finally
            {
                EndSubsegment();
            }
        }

        /// <summary>
        /// Trace a given asynchronous method that returns no value.  A subsegment will be created for this method.
        /// Any exception thrown by the method will be captured.
        /// </summary>
        /// <param name="name">The name of the trace subsegment for the method</param>
        /// <param name="method">The method to be traced</param>
        public async Task TraceMethodAsync(string name, Func<Task> method)
        {
            BeginSubsegment(name);

            try
            {
                await method();
            }
            catch (Exception e)
            {
                AddException(e);
                throw;
            }

            finally
            {
                EndSubsegment();
            }
        }

        /// <summary>
        /// Gets entity (segment/subsegment) from the <see cref="TraceContext"/>.
        /// </summary>
        /// <returns>The entity (segment/subsegment)</returns>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to get.</exception>
        public Entity GetEntity()
        {
            return TraceContext.GetEntity();
        }

        /// <summary>
        /// Set the specified entity (segment/subsegment) into <see cref="TraceContext"/>.
        /// </summary>
        /// <param name="entity">The entity to be set</param>
        /// <exception cref="EntityNotAvailableException">Thrown when the entity is not available to set</exception>
        public void SetEntity(Entity entity)
        {
            TraceContext.SetEntity(entity);
        }

        /// <summary>
        /// Checks whether entity is present in <see cref="TraceContext"/>.
        /// </summary>
        /// <returns>True if entity is present TraceContext else false.</returns>
        public bool IsEntityPresent()
        {
            return TraceContext.IsEntityPresent();
        }

        /// <summary>
        /// Clear entity from <see cref="TraceContext"/>.
        /// </summary>
        public void ClearEntity()
        {
            TraceContext.ClearEntity();
        }
    }
}
