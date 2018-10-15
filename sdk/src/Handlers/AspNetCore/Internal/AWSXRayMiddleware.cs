//-----------------------------------------------------------------------------
// <copyright file="AWSXRayMiddleware.cs" company="Amazon.com">
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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Amazon.XRay.Recorder.Handlers.AspNetCore.Internal
{
    /// <summary>
    /// The Middleware to intercept HTTP request for ASP.NET Core.
    /// For each request, <see cref="AWSXRayMiddleware"/> will try to parse trace header
    /// from HTTP request header, and determine if tracing is enabled. If enabled, it will
    /// start a new segment before invoking inner handler. And end the segment before it returns
    /// the response to outer handler.
    /// Note: This class should not be instantiated or used in anyway. It is used internally within SDK.
    /// </summary>
    public class AWSXRayMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayMiddleware));
        private readonly AWSXRayRecorder _recorder;
        private static readonly string X_FORWARDED_FOR = "X-Forwarded-For";

        /// <summary>
        /// Gets or sets the segment naming strategy.
        /// </summary>
        private SegmentNamingStrategy SegmentNamingStrategy { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayMiddleware" /> class with a provided instance of <see cref="AWSXRayRecorder" />.
        /// </summary>
        /// <param name="next">Instance of <see cref="RequestDelegate"/></param>
        /// <param name="segmentNamingStrategy">The segment naming strategy.</param>
        /// <param name="recorder">The provided instance of <see cref="AWSXRayRecorder" />.</param>
        /// <exception cref="ArgumentNullException">segmentNamingStrategy is null.</exception>
        public AWSXRayMiddleware(RequestDelegate next, SegmentNamingStrategy segmentNamingStrategy, AWSXRayRecorder recorder)
        {
            _next = next;
            SegmentNamingStrategy = segmentNamingStrategy ?? throw new ArgumentNullException("segmentNamingStrategy");
            _recorder = recorder ?? throw new ArgumentNullException("recorder");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayMiddleware" /> class.
        /// </summary>
        /// <param name="next">Instance of <see cref="RequestDelegate"/></param>
        /// <param name="segmentNamingStrategy">The segment naming strategy.</param>
        /// <param name="configuration">The instance of <see cref="IConfiguration" />.</param>
        /// <exception cref="ArgumentNullException">segmentNamingStrategy is null.</exception>
        public AWSXRayMiddleware(RequestDelegate next, SegmentNamingStrategy segmentNamingStrategy, IConfiguration configuration)
        {
            AWSXRayRecorder.InitializeInstance(configuration);
            _recorder = AWSXRayRecorder.Instance;
            _next = next;
            SegmentNamingStrategy = segmentNamingStrategy ?? throw new ArgumentNullException("segmentNamingStrategy");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayMiddleware"/> class.
        /// </summary>
        /// <param name="next">Instance of <see cref="RequestDelegate"/></param>
        /// <param name="fixedName">Name to be used for all generated segments.</param>
        public AWSXRayMiddleware(RequestDelegate next, string fixedName) : this(next, new FixedSegmentNamingStrategy(fixedName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayMiddleware"/> class.
        /// </summary>
        /// <param name="next">Instance of <see cref="RequestDelegate"/></param>
        /// <param name="fixedName">Name to be used for all generated segments.</param>
        /// <param name="configuration"><see cref="IConfiguration"/> instance.</param>
        public AWSXRayMiddleware(RequestDelegate next, string fixedName, IConfiguration configuration) : this(next, new FixedSegmentNamingStrategy(fixedName), configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSXRayMiddleware" /> class with default instance <see cref="AWSXRayRecorder" />.
        /// </summary>
        /// <param name="next">Instance of <see cref="RequestDelegate"/></param>
        /// <param name="segmentNamingStrategy">The segment naming strategy.</param>
        /// <exception cref="ArgumentNullException">segmentNamingStrategy is null.</exception>
        public AWSXRayMiddleware(RequestDelegate next, SegmentNamingStrategy segmentNamingStrategy) : this(next, segmentNamingStrategy, AWSXRayRecorder.Instance)
        {
        }

        /// <summary>
        /// Processes HTTP request and response. A segment is created at the beginning of the request and closed at the 
        /// end of the request. If the web app is running on AWS Lambda, a subsegment is started and ended for the respective 
        /// events.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            ProcessHTTPRequest(context);

            try
            {
                if (_next != null)
                {
                    await _next.Invoke(context); // call next handler
                }
            }
            catch (Exception exc)
            {
                _recorder.AddException(exc);
                throw;
            }

            finally
            {
                ProcessHTTPResponse(context);
            }
        }

        /// <summary>
        /// Processes HTTP response
        /// </summary>
        private void ProcessHTTPResponse(HttpContext context)
        {
            HttpResponse response = context.Response;

            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                var responseAttributes = new Dictionary<string, object>();
                PopulateResponseAttributes(response, responseAttributes);
                _recorder.AddHttpInformation("response", responseAttributes);
            }

            if (AWSXRayRecorder.IsLambda())
            {
                _recorder.EndSubsegment();
            }
            else
            {
                _recorder.EndSegment();
            }
        }

        private void PopulateResponseAttributes(HttpResponse response, Dictionary<string, object> responseAttributes)
        {
            int statusCode = (int)response.StatusCode;

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

            responseAttributes["status"] = statusCode;

            if (response.Headers.ContentLength != null)
            {
                responseAttributes["content_length"] = response.Headers.ContentLength;
            }
        }

        /// <summary>
        /// Processes HTTP request.
        /// </summary>
        private void ProcessHTTPRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            string headerString = null;

            if (request.Headers.TryGetValue(TraceHeader.HeaderKey, out StringValues headerValue))
            {
                if (headerValue.ToArray().Length >= 1)
                    headerString = headerValue.ToArray()[0];
            }

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

            var segmentName = SegmentNamingStrategy.GetSegmentName(request);
            bool isSampleDecisionRequested = traceHeader.Sampled == SampleDecision.Requested;

            string ruleName = null;
            // Make sample decision
            if (traceHeader.Sampled == SampleDecision.Unknown || traceHeader.Sampled == SampleDecision.Requested)
            {
                string host = request.Host.Host;
                string url = request.Path;
                string method = request.Method;
                SamplingInput samplingInput = new SamplingInput(host, url, method, segmentName, _recorder.Origin);
                SamplingResponse sampleResponse = _recorder.SamplingStrategy.ShouldTrace(samplingInput);
                traceHeader.Sampled = sampleResponse.SampleDecision;
                ruleName = sampleResponse.RuleName;
            }

            if (AWSXRayRecorder.IsLambda())
            {
                _recorder.BeginSubsegment(segmentName);
            }
            else
            {
                SamplingResponse samplingResponse = new SamplingResponse(ruleName, traceHeader.Sampled); // get final ruleName and SampleDecision
                _recorder.BeginSegment(SegmentNamingStrategy.GetSegmentName(request), traceHeader.RootTraceId, traceHeader.ParentId, samplingResponse);
            }

            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                var requestAttributes = new Dictionary<string, object>();
                PopulateRequestAttributes(request, requestAttributes);
                _recorder.AddHttpInformation("request", requestAttributes);
            }

            if (isSampleDecisionRequested)
            {
                context.Response.Headers.Add(TraceHeader.HeaderKey, traceHeader.ToString()); // Its recommended not to modify response header after _next.Invoke() call
            }
        }

        private static void PopulateRequestAttributes(HttpRequest request, Dictionary<string, object> requestAttributes)
        {
            requestAttributes["url"] = GetUrl(request);
            requestAttributes["method"] = request.Method;
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

            if (request.Headers.ContainsKey(HeaderNames.UserAgent))
            {
                requestAttributes["user_agent"] = request.Headers[HeaderNames.UserAgent].ToString();
            }
        }

        private static string GetUrl(HttpRequest request)
        {
            return Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request);
        }

        private static string GetXForwardedFor(HttpRequest request)
        {
            String clientIp = null;

            if (request.HttpContext.Request.Headers.TryGetValue(X_FORWARDED_FOR, out StringValues headerValue))
            {
                if (headerValue.ToArray().Length >= 1)
                    clientIp = headerValue.ToArray()[0];
            }

            return string.IsNullOrEmpty(clientIp) ? null : clientIp.Split(',')[0].Trim();
        }

        private static string GetClientIpAddress(HttpRequest request)
        {
            return request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
