//-----------------------------------------------------------------------------
// <copyright file="HttpWebRequestTracingExtension.cs" company="Amazon.com">
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
using System.Net;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Handlers.System.Net
{
    /// <summary>
    /// Tracing extension methods of <see cref="HttpWebRequest"/> class. 
    /// </summary>
    /// <see cref="HttpWebRequest"/>
    public static class HttpWebRequestTracingExtension
    {
        /// <summary>
        /// Wrapper of <see cref="WebRequest.GetResponse"/> method.
        /// It collects information from request and response. Also, a trace header will be injected 
        /// into the HttpWebRequest to propagate the tracing to downstream web service. This method is 
        /// used for synchronous requests.
        /// </summary>
        /// <param name="request">An instance of <see cref="WebRequest"/> which the method extended to</param>
        /// <returns>A <see cref="WebResponse"/> that contains the response from the Internet resource.</returns>
        public static WebResponse GetResponseTraced(this WebRequest request)
        {
            ProcessRequest(request);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                ProcessResponse(response);
                return response;
            }
            catch (Exception e)
            {
                AWSXRayRecorder.Instance.AddException(e);

                if (e is WebException webException)
                {
                    var exceptionResponse = (HttpWebResponse)webException.Response;

                    if (exceptionResponse != null)
                    {
                        ProcessResponse(exceptionResponse);
                    }
                }

                throw;
            }

            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        /// <summary>
        /// Wrapper of <see cref="WebRequest.GetResponseAsync"/> method.
        /// It collects information from request and response. Also, a trace header will be injected 
        /// into the HttpWebRequest to propagate the tracing to downstream web service. This method is
        /// used for asynchronous requests.
        /// </summary>
        /// <param name="request">An instance of <see cref="WebRequest"/> which the method extended to</param>
        /// <returns>A task of <see cref="WebResponse"/> that contains the response from the Internet resource.</returns>
        public static async Task<WebResponse> GetAsyncResponseTraced(this WebRequest request)
        {
            ProcessRequest(request);

            try
            {
                var response = (HttpWebResponse)await request.GetResponseAsync();
                ProcessResponse(response);
                return response;
            }
            catch (Exception e)
            {
                AWSXRayRecorder.Instance.AddException(e);

                if (e is WebException webException)
                {
                    var exceptionResponse = (HttpWebResponse)webException.Response;

                    if (exceptionResponse != null)
                    {
                        ProcessResponse(exceptionResponse);
                    }
                }

                throw;
            }

            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
        }

        private static void ProcessResponse(HttpWebResponse response)
        {
            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                Dictionary<string, object> responseInformation = new Dictionary<string, object>();
                int statusCode = (int)response.StatusCode;
                responseInformation["status"] = statusCode;

                if (statusCode >= 400 && statusCode <= 499)
                {
                    AWSXRayRecorder.Instance.MarkError();

                    if (statusCode == 429)
                    {
                        AWSXRayRecorder.Instance.MarkThrottle();
                    }
                }
                else if (statusCode >= 500 && statusCode <= 599)
                {
                    AWSXRayRecorder.Instance.MarkFault();
                }

                responseInformation["content_length"] = response.ContentLength;
                AWSXRayRecorder.Instance.AddHttpInformation("response", responseInformation);
            }
        }

        private static void ProcessRequest(WebRequest request)
        {
            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                AWSXRayRecorder.Instance.BeginSubsegment(request.RequestUri.Host);
                AWSXRayRecorder.Instance.SetNamespace("remote");

                Dictionary<string, object> requestInformation = new Dictionary<string, object>();
                requestInformation["url"] = request.RequestUri.AbsoluteUri;
                requestInformation["method"] = request.Method;
                AWSXRayRecorder.Instance.AddHttpInformation("request", requestInformation);
            }

            if (TraceHeader.TryParse(TraceContext.GetEntity(), out TraceHeader header))
            {
                request.Headers.Add(TraceHeader.HeaderKey, header.ToString());
            }
        }

        private static void ProcessException(int statusCode)
        {
            var recorder = AWSXRayRecorder.Instance;
            var responseAttributes = new Dictionary<string, object>();

            if (statusCode >= 400 && statusCode <= 499)
            {
                recorder.MarkError();

                if (statusCode == 429)
                {
                    recorder.MarkThrottle();
                }
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                recorder.MarkFault();
            }

            responseAttributes["status"] = statusCode;
            recorder.AddHttpInformation("response", responseAttributes);
        }
    }
}
