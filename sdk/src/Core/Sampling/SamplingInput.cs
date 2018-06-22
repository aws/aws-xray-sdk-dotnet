//-----------------------------------------------------------------------------
// <copyright file="SamplingInput.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Sampling input for "ShoudTrace" method.
    /// </summary>
    public class SamplingInput
    {
        public SamplingInput()
        {
        }

        public SamplingInput(string host, string url, string method, string serviceName, string serviceType)
        {
            Host = host;
            Url = url;
            Method = method;
            ServiceName = serviceName;
            ServiceType = serviceType;
        }

        /// <summary>
        /// Name of the host.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// URL path.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// HTTP Method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Service name of the request.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// ServiceType of the application.
        /// </summary>
        public string ServiceType { get; set; }
    }
}