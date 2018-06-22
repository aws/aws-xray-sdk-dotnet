//-----------------------------------------------------------------------------
// <copyright file="TracingMessageHandler.cs" company="Amazon.com">
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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;

namespace Amazon.XRay.Recorder.Handlers.AspNet
{
    /// <summary>
    /// The message handler to intercept HTTP request for ASP.NET Web API.
    /// For each request, <see cref="TracingMessageHandler"/> will try to parse trace header
    /// from HTTP request header, and determine if tracing is enabled. If enabled, it will
    /// start a new segment before invoking inner handler. And end the segment before it returns
    /// the response to outer handler.
    /// </summary>
    [Obsolete("TracingMessageHandler is deprecated, please use AWSXRayASPNET instead.")]
    public class TracingMessageHandler : DelegatingHandler
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(TracingMessageHandler));
        private readonly AWSXRayRecorder _recorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingMessageHandler"/> class.
        /// </summary>
        /// <param name="fixedName">Name of the fixed.</param>
        public TracingMessageHandler(string fixedName) : this(new FixedSegmentNamingStrategy(fixedName))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingMessageHandler" /> class with default instance <see cref="AWSXRayRecorder" />.
        /// </summary>
        /// <param name="segmentNamingStrategy">The segment naming strategy.</param>
        /// <exception cref="System.ArgumentNullException">segmentNamingStrategy is null.</exception>
        public TracingMessageHandler(SegmentNamingStrategy segmentNamingStrategy)
            : this(segmentNamingStrategy, AWSXRayRecorder.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingMessageHandler" /> class with a provided instance of <see cref="AWSXRayRecorder" />.
        /// </summary>
        /// <param name="segmentNamingStrategy">The segment naming strategy.</param>
        /// <param name="recorder">The provided instance of <see cref="AWSXRayRecorder" />.</param>
        /// <exception cref="System.ArgumentNullException">segmentNamingStrategy is null.</exception>
        public TracingMessageHandler(SegmentNamingStrategy segmentNamingStrategy, AWSXRayRecorder recorder)
        {
            SegmentNamingStrategy = segmentNamingStrategy ?? throw new ArgumentNullException("segmentNamingStrategy");
            _recorder = recorder ?? throw new ArgumentNullException("recorder");
        }

        /// <summary>
        /// Gets or sets the segment naming strategy.
        /// </summary>
        public SegmentNamingStrategy SegmentNamingStrategy { get; set; }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous
        /// operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>Returns System.Threading.Tasks.Task. The task object representing the asynchronous operation.</returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            string headerSring = null;
            string ruleName = null;
            if (request.Headers.TryGetValues(TraceHeader.HeaderKey, out IEnumerable<string> headerValue))
            {
                headerSring = headerValue.First();
            }

            // If Trace header doesn't exist, which means this is the root node. Create a new traceId and inject the trace header.
            if (!TraceHeader.TryParse(headerSring, out TraceHeader traceHeader))
            {
                _logger.DebugFormat("Trace header doesn't exist or not valid. Injecting a new one. existing header = {0}", headerSring);
                traceHeader = new TraceHeader
                {
                    RootTraceId = TraceId.NewId(),
                    ParentId = null,
                    Sampled = SampleDecision.Unknown
                };
            }

            bool isSampleDecisionRequested = traceHeader.Sampled == SampleDecision.Requested;

            string segmentName = SegmentNamingStrategy.GetSegmentName(request);

            // Make sample decision
            if (traceHeader.Sampled == SampleDecision.Unknown || traceHeader.Sampled == SampleDecision.Requested)
            {
                string host = request.Headers.Host;
                string url = request.RequestUri.AbsolutePath;
                string method = request.Method.Method;
                SamplingInput samplingInput = new SamplingInput(host, url, method, segmentName, _recorder.Origin);
                SamplingResponse s = _recorder.SamplingStrategy.ShouldTrace(samplingInput);
                traceHeader.Sampled = s.SampleDecision;
                ruleName = s.RuleName;
            }

            SamplingResponse samplingResponse = new SamplingResponse(ruleName,traceHeader.Sampled); // get final ruleName and SampleDecision
            _recorder.BeginSegment(segmentName, traceHeader.RootTraceId, traceHeader.ParentId, samplingResponse);

            if (!AppSettings.IsXRayTracingDisabled)
            {
                var requestAttributes = new Dictionary<string, object>();
                requestAttributes["url"] = request.RequestUri.AbsoluteUri;
                requestAttributes["method"] = request.Method.Method;
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

                requestAttributes["user_agent"] = request.Headers.UserAgent.ToString();
                _recorder.AddHttpInformation("request", requestAttributes);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (!AppSettings.IsXRayTracingDisabled)
            {
                var responseAttributes = new Dictionary<string, object>();
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

                if (response.Content != null && response.Content.Headers.ContentLength != null)
                {
                    responseAttributes["content_length"] = response.Content.Headers.ContentLength;
                }

                _recorder.AddHttpInformation("response", responseAttributes);
            }

            _recorder.EndSegment();

            // If the sample decision is requested, added the trace header to response
            if (isSampleDecisionRequested)
            {
                response.Headers.Add(TraceHeader.HeaderKey, traceHeader.ToString());
            }

            return response;
        }

        private static string GetXForwardedFor(HttpRequestMessage request)
        {
            string httpContext = "MS_HttpContext";
            if (!request.Properties.ContainsKey(httpContext))
            {
                return null;
            }

            dynamic context = request.Properties[httpContext];
            if (context == null)
            {
                return null;
            }

            string clientIp = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            return string.IsNullOrEmpty(clientIp) ? null : clientIp.Split(',').First().Trim();
        }

        private static string GetClientIpAddress(HttpRequestMessage request)
        {
            // Web-hosting
            string httpContext = "MS_HttpContext";
            if (request.Properties.ContainsKey(httpContext))
            {
                dynamic context = request.Properties[httpContext];
                if (context != null)
                {
                    return context.Request.UserHostAddress;
                }
            }

            // Self-hosting
            string remoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
            if (request.Properties.ContainsKey(remoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[remoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            // Self-hosting using Owin
            string owinContext = "MS_OwinContext";
            if (request.Properties.ContainsKey(owinContext))
            {
                dynamic context = request.Properties[owinContext];
                if (context != null)
                {
                    return context.Request.RemoteIpAddress;
                }
            }

            return null;
        }
    }
}
