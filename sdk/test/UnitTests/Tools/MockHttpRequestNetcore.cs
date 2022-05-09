//-----------------------------------------------------------------------------
// <copyright file="MockHttpRequestNetCore.cs" company="Amazon.com">
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

using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Transform;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Net;
using Amazon.Runtime.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    //Used for NetCore
    public sealed class MockHttpRequest : IHttpRequest<HttpContent>
    {
        private HttpClient _httpClient = new HttpClient();

        public MockHttpRequest(Uri requestUri, Action action, Func<MockHttpRequest, HttpResponseMessage> responseCreator = null)
        {
            this.RequestUri = requestUri;
            this.GetResponseAction = action;
            this.ResponseCreator = responseCreator ?? CreateResponse;
        }

        public bool IsDisposed { get; set; }

        public bool IsAborted { get; set; }

        public bool IsConfigureRequestCalled { get; set; }

        public bool IsSetRequestHeadersCalled { get; set; }

        public bool IsGetRequestContentCalled { get; set; }

        public string Method { get; set; }

        public Uri RequestUri { get; set; }

        public Action GetResponseAction { get; set; }

        public Func<MockHttpRequest, HttpResponseMessage> ResponseCreator { get; set; }

        private HttpResponseMessage CreateResponse(MockHttpRequest request)
        {
            // Extract the last segment of the URI, this is the custom URI 
            // sent by the unit tests.
            var resourceName = request.RequestUri.Host.Split('.').Last();
            var response = MockWebResponse.CreateFromResource(resourceName);

            if (response.StatusCode >= HttpStatusCode.OK && response.StatusCode <= (HttpStatusCode)299)
            {
                return response;
            }
            else
            {
                throw new HttpErrorResponseException(CustomWebResponse.GenerateWebResponse(response));
            }
        }

        public void Abort()
        {
            this.IsAborted = true;
        }

        public void ConfigureRequest(IRequestContext requestContext)
        {
            this.IsConfigureRequestCalled = true;
        }

        public void Dispose()
        {
            this.IsDisposed = true;
        }

        public HttpContent GetRequestContent()
        {
            this.IsGetRequestContentCalled = true;
            try
            {
                return new HttpRequestMessage().Content;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public Task<HttpContent> GetRequestContentAsync()
        {
            return Task.FromResult<HttpContent>(new HttpRequestMessage().Content);
        }

        public IWebResponseData GetResponse()
        {
            if (this.GetResponseAction != null)
            {
                this.GetResponseAction();
            }
            var response = ResponseCreator(this);
            return CustomWebResponse.GenerateWebResponse(response);
        }

        public Task<IWebResponseData> GetResponseAsync(CancellationToken cancellationToken)
        {
            if (this.GetResponseAction != null)
            {
                this.GetResponseAction();
            }
            var response = ResponseCreator(this);
            return Task.FromResult<IWebResponseData>(CustomWebResponse.GenerateWebResponse(response));
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            this.IsSetRequestHeadersCalled = true;
        }

        public Stream SetupProgressListeners(Stream originalStream, long progressUpdateInterval, object sender, EventHandler<StreamTransferProgressArgs> callback)
        {
            return originalStream;
        }

        public void WriteToRequestBody(HttpContent requestContent, Stream contentStream, IDictionary<string, string> contentHeaders, IRequestContext requestContext)
        {
            Assert.IsNotNull(contentStream);
            Assert.IsNotNull(contentHeaders);
            Assert.IsNotNull(requestContext);
        }

        public void WriteToRequestBody(HttpContent requestContent, byte[] content, IDictionary<string, string> contentHeaders)
        {
            Assert.IsNotNull(content);
            Assert.IsNotNull(contentHeaders);
        }
    }
}
