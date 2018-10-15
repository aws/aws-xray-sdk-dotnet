using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Handlers.System.Net.Utils
{
    public static class RequestUtil
    {
        /// <summary>
        /// Collects information from and adds a tracing header to the request.
        /// </summary>
        /// <param name="request">An instance of <see cref="WebRequest"/></param>
        internal static void ProcessRequest(WebRequest request)
        {
            ProcessRequest(request.RequestUri, request.Method, header => request.Headers.Add(TraceHeader.HeaderKey, header));
        }
        /// <summary>
        /// Collects information from and adds a tracing header to the request.
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpRequestMessage"/></param>
        internal static void ProcessRequest(HttpRequestMessage request)
        {
            ProcessRequest(request.RequestUri, request.Method.Method, AddOrReplaceHeader);

            void AddOrReplaceHeader(string header)
            {
                request.Headers.Remove(TraceHeader.HeaderKey);
                request.Headers.Add(TraceHeader.HeaderKey, header);
            }
        }

        /// <summary>
        /// Collects information from the response and adds to <see cref="AWSXRayRecorder"/> instance.
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpWebResponse"/></param>
        internal static void ProcessResponse(HttpWebResponse response)
        {
            ProcessResponse(response.StatusCode, response.ContentLength);
        }

        /// <summary>
        /// Collects information from the response and adds to <see cref="AWSXRayRecorder"/> instance.
        /// </summary>
        /// <param name="request">An instance of <see cref="HttpResponseMessage"/></param>
        internal static void ProcessResponse(HttpResponseMessage response)
        {
            ProcessResponse(response.StatusCode, response.Content.Headers.ContentLength);
        }

        private static void ProcessRequest(Uri uri, string method, Action<string> addHeaderAction)
        {
            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                AWSXRayRecorder.Instance.BeginSubsegment(uri.Host);
                AWSXRayRecorder.Instance.SetNamespace("remote");

                var requestInformation = new Dictionary<string, object>
                {
                    ["url"] = uri.AbsoluteUri,
                    ["method"] = method
                };
                AWSXRayRecorder.Instance.AddHttpInformation("request", requestInformation);
            }

            if (TraceHeader.TryParse(AWSXRayRecorder.Instance.TraceContext.GetEntity(), out var header))
            {
                addHeaderAction(header.ToString());
            }
        }

        private static void ProcessResponse(HttpStatusCode httpStatusCode, long? contentLength)
        {
            if (!AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                var statusCode = (int)httpStatusCode;

                var responseInformation = new Dictionary<string, object> { ["status"] = statusCode };
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

                responseInformation["content_length"] = contentLength;
                AWSXRayRecorder.Instance.AddHttpInformation("response", responseInformation);
            }
        }
    }
}
