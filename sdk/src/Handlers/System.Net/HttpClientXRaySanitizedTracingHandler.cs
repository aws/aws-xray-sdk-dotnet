//-----------------------------------------------------------------------------
// <copyright file="HttpClientTracingHandler.cs" company="Amazon.com">
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.System.Net.Utils;

namespace Amazon.XRay.Recorder.Handlers.System.Net
{
    /// <summary>
    /// Wrapper around <see cref="HttpClientHandler"/> for AWS X-Ray tracing of HTTP requests that will strip query parameters when tracing
    /// </summary>
    public class HttpClientXRaySanitizedTracingHandler : DelegatingHandler
    {
        private bool _sanitizeHttpRequestTracing { get; set; } = true;

        public HttpClientXRaySanitizedTracingHandler() : base()
        {
        }

        public HttpClientXRaySanitizedTracingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        /// <summary>
        /// Wrapper of <see cref="HttpClientHandler.SendAsync"/> method.
        /// It collects information from request and response. Also, a trace header will be injected 
        /// into the HttpWebRequest to propagate the tracing to downstream web service. 
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpResponseMessage"/></param>
        /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A Task of <see cref="HttpResponseMessage"/> representing the asynchronous operation</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUtil.ProcessRequest(request, _sanitizeHttpRequestTracing);
            
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                RequestUtil.ProcessResponse(response);
            }
            catch (Exception ex)
            {
                AWSXRayRecorder.Instance.AddException(ex);
                throw;
            }
            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
            return response;
        }
    }
}
