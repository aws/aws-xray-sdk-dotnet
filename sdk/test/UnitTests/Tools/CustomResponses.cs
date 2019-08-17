//-----------------------------------------------------------------------------
// <copyright file="CustomResponses.cs" company="Amazon.com">
//      Copyright 2017 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.IO;
using System.Net;
using System.Net.Http;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using Amazon.Util;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public static class CustomResponses
    {
#if NET452
        public static void SetResponse(
            AmazonServiceClient client, string content, string requestId, bool isOK)
        {
            var response = Create(content, requestId, isOK);
            SetResponse(client, response);
        }

        public static void SetResponse(
            AmazonServiceClient client,
            Func<MockHttpRequest, HttpWebResponse> responseCreator)
        {
            var pipeline = client
                .GetType()
                .GetProperty("RuntimePipeline", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(client, null)
                as RuntimePipeline;

            var requestFactory = new MockHttpRequestFactory();
            requestFactory.ResponseCreator = responseCreator;
            var httpHandler = new HttpHandler<Stream>(requestFactory, client);
            pipeline.ReplaceHandler<HttpHandler<Stream>>(httpHandler);
        }

        private static Func<MockHttpRequest, HttpWebResponse> Create(
            string content, string requestId, bool isOK)
        {
            var status = isOK ? HttpStatusCode.OK : HttpStatusCode.NotFound;

            return (request) =>
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(requestId))
                {
                    headers.Add(HeaderKeys.RequestIdHeader, requestId);
                }
                var response = MockWebResponse.Create(status, headers, content);

                if (isOK)
                {
                    return response;
                }

                throw new HttpErrorResponseException(new HttpWebRequestResponseData(response));
            };
        }
#else

        public static void SetResponse(AmazonServiceClient client, string content, string requestId, bool isOK)
        {
            var response = Create(content, requestId, isOK);
            SetResponse(client, response);
        }

        public static void SetResponse(AmazonServiceClient client, Func<MockHttpRequest, HttpResponseMessage> responseCreator)
        {
            var pipeline = client
                .GetType()
                .GetProperty("RuntimePipeline", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(client, null)
                as RuntimePipeline;

            var requestFactory = new MockHttpRequestFactory();
            requestFactory.ResponseCreator = responseCreator;
            var httpHandler = new HttpHandler<HttpContent>(requestFactory, client);
            pipeline.ReplaceHandler<HttpHandler<HttpContent>>(httpHandler);
        }

        private static Func<MockHttpRequest, HttpResponseMessage> Create(
            string content, string requestId, bool isOK)
        {
            var status = isOK ? HttpStatusCode.OK : HttpStatusCode.NotFound;

            return (request) =>
            {
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.Ordinal);
                if (!string.IsNullOrEmpty(requestId))
                {
                    headers.Add(HeaderKeys.RequestIdHeader, requestId);
                }
                var response = MockWebResponse.Create(status, headers, content);

                if (isOK)
                {
                    return response;
                }

                throw new HttpErrorResponseException(CustomWebResponse.GenerateWebResponse(response));
            };
        }
#endif
    }

}
