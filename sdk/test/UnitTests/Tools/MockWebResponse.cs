//-----------------------------------------------------------------------------
// <copyright file="MockWebResponse.cs" company="Amazon.com">
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
using System.Net.Http;
using System.Reflection;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public class MockWebResponse
    {
#if NET45
        public static HttpWebResponse CreateFromResource(string resourceName)
        {
            var rawResponse = Utils.GetResourceText(resourceName);
            var response = ParseRawReponse(rawResponse);
            var statusCode = ParseStatusCode(response.StatusLine);
            return Create(statusCode, response.Headers, response.Body);
        }

        public static HttpWebResponse Create(HttpStatusCode statusCode, IDictionary<string, string> headers, string body = null)
        {
            var type = typeof(HttpWebResponse);
            var assembly = Assembly.GetAssembly(type);
            var obj = assembly.CreateInstance("System.Net.HttpWebResponse");

            var webHeaders = new WebHeaderCollection();
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    webHeaders.Add(header.Key, header.Value);
                }
            }

            body = body ?? string.Empty;
            Stream responseBodyStream = Utils.CreateStreamFromString(body);

            var statusFieldInfo = type.GetField(
                "m_StatusCode",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var headersFieldInfo = type.GetField(
                "m_HttpResponseHeaders",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var streamFieldInfo = type.GetField(
                "m_ConnectStream",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var contentLengthFieldInfo = type.GetField(
                "m_ContentLength",
                BindingFlags.NonPublic | BindingFlags.Instance);

            statusFieldInfo.SetValue(obj, statusCode);
            headersFieldInfo.SetValue(obj, webHeaders);
            streamFieldInfo.SetValue(obj, responseBodyStream);
            contentLengthFieldInfo.SetValue(obj, responseBodyStream.Length);

            return obj as HttpWebResponse;
            
        }
#else
        public static HttpResponseMessage CreateFromResource(string resourceName)
        {
            var rawResponse = Utils.GetResourceText(resourceName);

            var response = ParseRawReponse(rawResponse);
            var statusCode = ParseStatusCode(response.StatusLine);

            return Create(statusCode, response.Headers, response.Body);
        }

        public static HttpResponseMessage Create(HttpStatusCode statusCode, IDictionary<string, string> headers, string body = null)
        {
            var type = typeof(HttpResponseMessage);
            var assembly = Assembly.GetAssembly(type);
            var obj = assembly.CreateInstance("System.Net.Http.HttpResponseMessage");

            HttpResponseMessage httpResponseMessage = obj as HttpResponseMessage;
            var webHeaders = new WebHeaderCollection();
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    webHeaders.Add(header.Key, header.Value);
                    httpResponseMessage.Headers.Add(header.Key, header.Value);
                }
            }

            body = body ?? string.Empty;
            _ = Utils.CreateStreamFromString(body);
            httpResponseMessage.StatusCode = statusCode;
            StreamReader streamReader = new StreamReader($"JSONs{Path.DirectorySeparatorChar}FakeResponse.json");
            string json = streamReader.ReadToEnd();
            httpResponseMessage.Content = new StringContent(json); // Content should be in Json format else we get exception from downstream unmarshalling
            return httpResponseMessage;
        }

#endif
        public static HttpResponse ParseRawReponse(string rawResponse)
        {
            var response = new HttpResponse();
            response.StatusLine = rawResponse;

            var responseLines = rawResponse.Split('\n');

            if (responseLines.Length == 0)
            {
                throw new ArgumentException(
                    "The resource does not contain a valid HTTP response.",
                    nameof(rawResponse));
            }

            response.StatusLine = responseLines[0];
            var currentLine = responseLines[0];

/* Unmerged change from project 'AWSXRayRecorder.UnitTests (netcoreapp2.0)'
Before:
            var statusCode = ParseStatusCode(currentLine);
After:
            HttpStatusCode statusCode;
            _ = ParseStatusCode(currentLine);
*/

/* Unmerged change from project 'AWSXRayRecorder.UnitTests (netcoreapp3.1)'
Before:
            var statusCode = ParseStatusCode(currentLine);
After:
            HttpStatusCode statusCode;
            _ = ParseStatusCode(currentLine);
*/
            _ = ParseStatusCode(currentLine);
            if (responseLines.Length > 1)
            {

                int lineIndex;
                for (lineIndex = 1; lineIndex < responseLines.Length; lineIndex++)
                {
                    currentLine = responseLines[lineIndex];
                    if (currentLine.Trim() == string.Empty)
                    {
                        currentLine = responseLines[lineIndex - 1];
                        break;
                    }

                    var index = currentLine.IndexOf(":");
                    if (index != -1)
                    {
                        var headerKey = currentLine.Substring(0, index);
                        var headerValue = currentLine.Substring(index + 1);
                        response.Headers.Add(headerKey.Trim(), headerValue.Trim());
                    }
                }
            }

            var startOfBody = rawResponse.IndexOf(currentLine) + currentLine.Length;
            response.Body = rawResponse.Substring(startOfBody).Trim();
            return response;
        }

        private static HttpStatusCode ParseStatusCode(string statusLine)
        {
            try
            {
                string statusCode = statusLine.Split(' ')[1];
                return (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), statusCode);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Invalid HTTP status line.", exception);
            }
        }

        public class HttpResponse
        {
            public HttpResponse()
            {
                this.Headers = new Dictionary<string, string>();
            }

            public string StatusLine { get; set; }

            public IDictionary<string, string> Headers { get; private set; }

            public string Body { get; set; }
        }
    }
}
