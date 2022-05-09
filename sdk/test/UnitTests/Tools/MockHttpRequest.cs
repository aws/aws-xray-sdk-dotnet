//-----------------------------------------------------------------------------
// <copyright file="MockHttpRequest.cs" company="Amazon.com">
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public sealed class MockHttpRequest : IHttpRequest<Stream>
    {
        private Stream requestStream = null;
        public MockHttpRequest(Uri requestUri, Action action, Func<MockHttpRequest, HttpWebResponse> responseCreator = null)
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

        public Func<MockHttpRequest, HttpWebResponse> ResponseCreator { get; set; }

        public void ConfigureRequest(IRequestContext requestContext)
        {
            this.IsConfigureRequestCalled = true;
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            this.IsSetRequestHeadersCalled = true;
        }

        public Stream GetRequestContent()
        {
            this.IsGetRequestContentCalled = true;
            requestStream = new MemoryStream();
            return requestStream;
        }

        public Amazon.Runtime.Internal.Transform.IWebResponseData GetResponse()
        {
            if (this.GetResponseAction != null)
            {
                this.GetResponseAction();
            }

            var response = ResponseCreator(this);
            return new HttpWebRequestResponseData(response);
        }

        public void WriteToRequestBody(
            Stream requestContent, Stream contentStream, IDictionary<string, string> contentHeaders, IRequestContext requestContext)
        {
            Assert.IsNotNull(requestContent);
            Assert.IsNotNull(contentStream);
            Assert.IsNotNull(contentHeaders);
            Assert.IsNotNull(requestContext);
        }

        public void WriteToRequestBody(Stream requestContent, byte[] content, IDictionary<string, string> contentHeaders)
        {
            Assert.IsNotNull(requestContent);
            Assert.IsNotNull(content);
            Assert.IsNotNull(contentHeaders);
        }

        public void Abort()
        {
            this.IsAborted = true;
        }

        public Task<Stream> GetRequestContentAsync()
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task<IWebResponseData> GetResponseAsync(System.Threading.CancellationToken cancellationToken)
        {
            if (this.GetResponseAction != null)
            {
                this.GetResponseAction();
            }
            var response = ResponseCreator(this);
            return Task.FromResult<IWebResponseData>(new HttpWebRequestResponseData(response));
        }

        public void Dispose()
        {
            this.IsDisposed = true;
        }

        public Stream SetupProgressListeners(Stream originalStream, long progressUpdateInterval, object sender, EventHandler<StreamTransferProgressArgs> callback)
        {
            return originalStream;
        }

        private HttpWebResponse CreateResponse(MockHttpRequest request)
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
                throw new HttpErrorResponseException(new HttpWebRequestResponseData(response));

            }
        }

        public Task<Stream> GetRequestContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task WriteToRequestBodyAsync(Stream requestContent, Stream contentStream, IDictionary<string, string> contentHeaders, IRequestContext requestContext)
        {
            Assert.IsNotNull(requestContent);
            Assert.IsNotNull(contentStream);
            Assert.IsNotNull(contentHeaders);
            Assert.IsNotNull(requestContext);
            return Task.FromResult(0);
        }

        public Task WriteToRequestBodyAsync(Stream requestContent, byte[] requestData, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            Assert.IsNotNull(requestContent);
            Assert.IsNotNull(requestData);
            Assert.IsNotNull(headers);
            return Task.FromResult(0);
        }
    }
}
