//-----------------------------------------------------------------------------
// <copyright file="AWSXRayASPNET.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Strategies;
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Exceptions;
using System.Threading;
using Amazon.XRay.Recorder.Core.Internal.Context;

namespace Amazon.XRay.Recorder.Handlers.AspNet
{
    /// <summary>
    /// The class to intercept HTTP request for ASP.NET Framework.
    /// For each request, <see cref="AWSXRayASPNET"/> will try to parse trace header
    /// from HTTP request header, and determine if tracing is enabled. If enabled, it will
    /// start a new segment before invoking inner handler. And end the segment before it returns
    /// the response to outer handler.
    /// </summary>
    public class AWSXRayASPNET
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayASPNET));
        private static SegmentNamingStrategy segmentNamingStrategy;
        private static readonly AWSXRayRecorder _recorder;

        static AWSXRayASPNET()
        {
            if (!AWSXRayRecorder.IsCustomRecorder) // If custom recorder is not set
            {
               AWSXRayRecorder.Instance.SetTraceContext(new HybridContextContainer()); // configure Trace Context
            }
            _recorder = AWSXRayRecorder.Instance;
        }

        /// <summary>
        /// Key name that is used to store segment in the HttpApplication.Context object of the request.
        /// </summary>
        public const String XRayEntity = HybridContextContainer.XRayEntity;

        private static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets or sets the segment naming strategy.
        /// </summary>
        private static SegmentNamingStrategy GetSegmentNamingStrategy()
        {
            rwLock.EnterReadLock();
            try
            {
                // It is safe for this thread to read from the shared resource.
                return segmentNamingStrategy;
            }
            finally
            {
                rwLock.ExitReadLock(); // Ensure that the lock is released.
            }
        }

        /// <summary>
        /// Gets or sets the segment naming strategy.
        /// </summary>
        private static void SetSegmentNamingStrategy(SegmentNamingStrategy value)
        {
            rwLock.EnterWriteLock();
            try
            {
                // It is safe for this thread to write to the shared resource.
                segmentNamingStrategy = value;
            }
            finally
            {
                rwLock.ExitWriteLock(); // Ensure that the lock is released.
            }
        }

        private static void InitializeASPNET(string fixedName)
        {
            if (GetSegmentNamingStrategy() == null) // ensures only one time initialization among many HTTPApplication instances
            {
                InitializeASPNET(new FixedSegmentNamingStrategy(fixedName));
            }
        }

        private static void InitializeASPNET(SegmentNamingStrategy segmentNamingStrategy)
        {
            if (segmentNamingStrategy == null)
            {
                throw new ArgumentNullException("segmentNamingStrategy");
            }

            if (GetSegmentNamingStrategy() == null) // ensures only one time initialization among many HTTPApplication instances
            {
                SetSegmentNamingStrategy(segmentNamingStrategy);
            }

        }

        /// <summary>
        /// Registers X-Ray for the current object of  <see cref="HttpApplication"/> class. <see cref="HttpApplication.BeginRequest"/>, 
        /// <see cref="HttpApplication.EndRequest"/>, <see cref="HttpApplication.Error"/> event handlers are registered with X-Ray function.
        /// A segment is created at the beginning of the request and closed at the end of the request.
        /// </summary>
        /// <param name="httpApplication">Instance of  <see cref="HttpApplication"/> class.</param>
        /// <param name="fixedName">Name to be used for all generated segments.</param>
        public static void RegisterXRay(HttpApplication httpApplication, string segmentName)
        {
            InitializeASPNET(segmentName);
            httpApplication.BeginRequest += ProcessHTTPRequest;
            httpApplication.EndRequest += ProcessHTTPResponse;
            httpApplication.Error += ProcessHTTPError;
        }

        /// <summary>
        /// Registers X-Ray for the current object of  <see cref="HttpApplication"/> class. <see cref="HttpApplication.BeginRequest"/>, 
        /// <see cref="HttpApplication.EndRequest"/>, <see cref="HttpApplication.Error"/> event handlers are registered with X-Ray function.
        /// </summary>
        /// <param name="httpApplication">Instance of  <see cref="HttpApplication"/> class.</param>
        /// <param name="segmentNamingStrategy">Instance of  <see cref="SegmentNamingStrategy"/> class. Defines segment naming strategy.</param>
        public static void RegisterXRay(HttpApplication httpApplication, SegmentNamingStrategy segmentNamingStrategy)
        {
            InitializeASPNET(segmentNamingStrategy);
            httpApplication.BeginRequest += ProcessHTTPRequest;
            httpApplication.EndRequest += ProcessHTTPResponse;
            httpApplication.Error += ProcessHTTPError;
        }

        /// <summary>
        /// Processes HTTP request.
        /// </summary>
        private static void ProcessHTTPRequest(Object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;

            string ruleName = null;

            var request = context.Request;
            TraceHeader traceHeader = GetTraceHeader(context);

            var segmentName = GetSegmentNamingStrategy().GetSegmentName(request);
            // Make sample decision
            if (traceHeader.Sampled == SampleDecision.Unknown || traceHeader.Sampled == SampleDecision.Requested)
            {
                SamplingResponse response = MakeSamplingDecision(request, traceHeader,segmentName);
                ruleName = response.RuleName;
            }

            var timestamp = context.Timestamp.ToUniversalTime(); // Gets initial timestamp of current HTTP Request

            SamplingResponse samplingResponse = new SamplingResponse(ruleName, traceHeader.Sampled); // get final ruleName and SampleDecision
            _recorder.BeginSegment(segmentName, traceHeader.RootTraceId, traceHeader.ParentId, samplingResponse, timestamp);

            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                Dictionary<string, object> requestAttributes = new Dictionary<string, object>();
                ProcessRequestAttributes(request, requestAttributes);
                _recorder.AddHttpInformation("request", requestAttributes);
            }
        }

        private static void ProcessRequestAttributes(HttpRequest request, Dictionary<string, object> requestAttributes)
        {
            requestAttributes["url"] = request.Url.AbsoluteUri;
            requestAttributes["user_agent"] = request.UserAgent;
            requestAttributes["method"] = request.HttpMethod;
            string xForwardedFor = GetXForwardedFor(request);

            if (xForwardedFor == null)
            {
                requestAttributes["client_ip"] = GetClientIpAddress(request);
            }
            else
            {
                requestAttributes["client_ip"] = xForwardedFor;
                requestAttributes["x_forwarded_for"] = true;
            }
        }

        /// <summary>
        /// Processes HTTP response.
        /// </summary>
        private static void ProcessHTTPResponse(Object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;
            var response = context.Response;

            if (!AWSXRayRecorder.Instance.IsTracingDisabled() && response != null)
            {
                Dictionary<string, object> responseAttributes = new Dictionary<string, object>();
                ProcessResponseAttributes(response, responseAttributes);
                _recorder.AddHttpInformation("response", responseAttributes);
            }

            Exception exc = context.Error; // Record exception, if any

            if (exc != null)
            {
                _recorder.AddException(exc);
            }

            TraceHeader traceHeader = GetTraceHeader(context);
            bool isSampleDecisionRequested = traceHeader.Sampled == SampleDecision.Requested;

            if (traceHeader.Sampled == SampleDecision.Unknown || traceHeader.Sampled == SampleDecision.Requested)
            {
                SetSamplingDecision(traceHeader); // extracts sampling decision from the available segment
            }

            _recorder.EndSegment();
            // if the sample decision is requested, add the trace header to response
            if (isSampleDecisionRequested)
            {
                response.Headers.Add(TraceHeader.HeaderKey, traceHeader.ToString());
            }
        }

        private static void SetSamplingDecision(TraceHeader traceHeader)
        {
            try
            {
                Segment segment = (Segment)AWSXRayRecorder.Instance.GetEntity();
                traceHeader.Sampled = segment.Sampled;
            }
         
            catch (InvalidCastException e)
            {
                _logger.Error(new EntityNotAvailableException("Failed to cast the entity to Segment.", e), "Failed to  get the segment from trace context for setting sampling decision in the response.");
            }
        }

        private static void ProcessResponseAttributes(HttpResponse response, Dictionary<string, object> reponseAttributes)
        {
            int statusCode = (int)response.StatusCode;
            reponseAttributes["status"] = statusCode;

            if (statusCode >= 400 && statusCode <= 499)
            {
                _recorder.MarkError();

                if (statusCode == 429)
                {
                    _recorder.MarkThrottle();
                }
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                _recorder.MarkFault();
            }
        }

        private static SamplingResponse MakeSamplingDecision(HttpRequest request, TraceHeader traceHeader, string name)
        {
            string host = request.Headers.Get("Host");
            string url = request.Url.AbsolutePath;
            string method = request.HttpMethod;
            SamplingInput samplingInput = new SamplingInput(host, url, method, name, _recorder.Origin);
            SamplingResponse sampleResponse = _recorder.SamplingStrategy.ShouldTrace(samplingInput);
            traceHeader.Sampled = sampleResponse.SampleDecision;
            return sampleResponse;
        }

        /// <summary>
        /// Processes HTTP Error.
        /// NOTE : if we receive unhandled exception in BeginRequest() of any class implementing <see cref="IHttpModule"/> Interface, BeginRequest() 
        /// of the current <see cref="HttpApplication"/> is not executed (so no segment is created at this point).
        /// </summary>
        private static void ProcessHTTPError(Object sender, EventArgs e)
        {
            ProcessHTTPRequest(sender, e);
        }

        /// <summary>
        /// Returns instance of <see cref="TraceHeader"/> class from given <see cref="HttpContext"/> object.
        /// </summary>
        private static TraceHeader GetTraceHeader(HttpContext context)
        {
            var request = context.Request;
            string headerString = request.Headers.Get(TraceHeader.HeaderKey);

            // Trace header doesn't exist, which means this is the root node. Create a new traceId and inject the trace header.
            if (!TraceHeader.TryParse(headerString, out TraceHeader traceHeader))
            {
                _logger.DebugFormat("Trace header doesn't exist or not valid : ({0}). Injecting a new one.", headerString);
                traceHeader = new TraceHeader
                {
                    RootTraceId = TraceId.NewId(),
                    ParentId = null,
                    Sampled = SampleDecision.Unknown
                };
            }

            return traceHeader;
        }

        private static string GetXForwardedFor(HttpRequest request)
        {
            string clientIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            return string.IsNullOrEmpty(clientIp) ? null : clientIp.Split(',').First().Trim();
        }

        private static string GetClientIpAddress(HttpRequest request)
        {
            return request.UserHostAddress;
        }
    }
}
