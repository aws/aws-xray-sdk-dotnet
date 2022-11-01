//-----------------------------------------------------------------------------
// <copyright file="EC2Plugin.cs" company="Amazon.com">
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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.SQSEvents;
using Amazon.Runtime.Internal.Util;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Core.Lambda
{
    public class SQSMessageHelper
    {
        public static bool IsSampled(SQSEvent.SQSMessage message)
        {
            if (!message.Attributes.ContainsKey("AWSTraceHeader"))
            {
                return false;
            }

            return message.Attributes["AWSTraceHeader"].Contains("Sampled=1");
        }
    }
}
