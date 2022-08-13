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
using System.Net;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.System.Net.Utils;

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
        /// Request query string can be omitted from the http tracing depending on sanitizeHttpRequestTracing flag. Defaults to false, thus tracing absolute Uri.
        /// </summary>
        /// <param name="request">An instance of <see cref="WebRequest"/> which the method extended to</param>
        /// <param name="sanitizeHttpRequestTracing"> <see cref="Boolean"/> value.</param>
        /// <returns>A <see cref="WebResponse"/> that contains the response from the Internet resource.</returns>
        public static WebResponse GetResponseTraced(this WebRequest request, bool sanitizeHttpRequestTracing = false)
        {
            RequestUtil.ProcessRequest(request, sanitizeHttpRequestTracing);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                RequestUtil.ProcessResponse(response);
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
                        RequestUtil.ProcessResponse(exceptionResponse);
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
        /// Request query string can be omitted from the http tracing depending on sanitizeHttpRequestTracing flag. Defaults to false, thus tracing absolute Uri.
        /// </summary>
        /// <param name="request">An instance of <see cref="WebRequest"/> which the method extended to</param>
        /// <param name="sanitizeHttpRequestTracing"> <see cref="Boolean"/> value.</param>
        /// <returns>A task of <see cref="WebResponse"/> that contains the response from the Internet resource.</returns>
        public static async Task<WebResponse> GetAsyncResponseTraced(this WebRequest request, bool sanitizeHttpRequestTracing = false)
        {
            RequestUtil.ProcessRequest(request, sanitizeHttpRequestTracing);

            try
            {
                var response = (HttpWebResponse)await request.GetResponseAsync();
                RequestUtil.ProcessResponse(response);
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
                        RequestUtil.ProcessResponse(exceptionResponse);
                    }
                }

                throw;
            }

            finally
            {
                AWSXRayRecorder.Instance.EndSubsegment();
            }
        }
    }
}
