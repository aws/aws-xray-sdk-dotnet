//-----------------------------------------------------------------------------
// <copyright file="MockHttpRequestFactory.cs" company="Amazon.com">
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
using System.IO;
using System.Net;
using Amazon.Runtime;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    /// <summary>
    /// Not thread safe, initialize once for each test.
    /// </summary>
    public sealed class MockHttpRequestFactory : IHttpRequestFactory<Stream>
    {
        public Action GetResponseAction { get; set; }
        public Func<MockHttpRequest, HttpWebResponse> ResponseCreator { get; set; }

        public MockHttpRequest LastCreatedRequest { get; private set; }

        public IHttpRequest<Stream> CreateHttpRequest(Uri requestUri)
        {
            this.LastCreatedRequest = new MockHttpRequest(requestUri, this.GetResponseAction, this.ResponseCreator);
            return this.LastCreatedRequest;
        }

        public void Dispose()
        {
        }
    }
}
