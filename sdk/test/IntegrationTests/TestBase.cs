//-----------------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Amazon.com">
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

using System.Collections.Generic;
using System.Threading;
using Amazon.XRay;
using Amazon.XRay.Model;
using Amazon.XRay.Recorder.Core;

namespace Amazon.XRay.Recorder.IntegrationTests
{
    public class TestBase
    {
        protected static AmazonXRayClient XrayClient { get; set; }

        protected static AWSXRayRecorder Recorder { get; set; }

        protected static void ClassInit()
        {
            var config = new AmazonXRayConfig();
            config.ServiceURL = "https://xray.us-east-1.amazonaws.com ";
            config.AuthenticationRegion = "us-east-1";

            XrayClient = new AmazonXRayClient(config);
            Recorder = new AWSXRayRecorder();
        }

        protected static void ClassCleanup()
        {
            Recorder.Dispose();
        }

#if NET45
        protected BatchGetTracesResponse BatchGetTraces(string traceId)
        {
            var request = new BatchGetTracesRequest();

            request.TraceIds = new List<string>() { traceId };

            int retries = 0;
            BatchGetTracesResponse response = null;

            while (retries < 60)
            {
                response = XrayClient.BatchGetTraces(request);
                if (response.Traces.Count > 0)
                {
                    break;
                }
                else
                {
                    retries++;
                    Thread.Sleep(500);
                }
            }

            return response;
        }
#else
        protected async System.Threading.Tasks.Task<BatchGetTracesResponse> BatchGetTracesAsync(string traceId)
        {
            var request = new BatchGetTracesRequest();
            request.TraceIds = new List<string>() { traceId };

            int retries = 0;
            BatchGetTracesResponse response = null;

            // Retry for 30s
            while (retries < 60)
            {
                response = await XrayClient.BatchGetTracesAsync(request);
                if (response.Traces.Count > 0)
                {
                    break;
                }
                else
                {
                    retries++;
                    Thread.Sleep(500);
                }
            }
        
        return response;
        }
#endif
    }
}
