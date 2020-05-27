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
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Core.Plugins
{
    /// <summary>
    /// This is a plugin for EC2.
    /// </summary>
    public class EC2Plugin : IPlugin
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(EC2Plugin));
        private static readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// Gets the name of the origin associated with this plugin.
        /// </summary>
        /// <returns>The name of the origin associated with this plugin.</returns>
        public string Origin
        {
            get
            {
                return @"AWS::EC2::Instance";
            }
        }

        /// <summary>
        /// Gets the name of the service associated with this plugin.
        /// </summary>
        /// <returns>The name of the service that this plugin is associated with.</returns>
        public string ServiceName
        {
            get
            {
                return @"ec2";
            }
        }

        /// <summary>
        /// Gets the context of the runtime that this plugin is associated with.
        /// </summary>
        /// <param name="context">When the method returns, contains the runtime context of the plugin, or null if the runtime context is not available.</param>
        /// <returns>true if the runtime context is available; Otherwise, false.</returns>
        public bool TryGetRuntimeContext(out IDictionary<string, object> context)
        {
            context = null;
            var dict = new Dictionary<string, object>();
            const string metadata_base_url = "http://169.254.169.254/latest/";

            try
            {
                // try IMDSv2 endpoint for metadata
                // get the token
                Dictionary<string, string> header = new Dictionary<string, string>(1);
                header.Add("X-aws-ec2-metadata-token-ttl-seconds", "60");
                string token = DoRequest(metadata_base_url + "api/token", HttpMethod.Put, header).Result;

                header = new Dictionary<string, string>(1);
                header.Add("X-aws-ec2-metadata-token", token);

                string resp = DoRequest(metadata_base_url + "dynamic/instance-identity/document", HttpMethod.Get, header).Result;
                dict = ParseMetadata(resp);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception occurred while getting EC2 metadata from IMDSv2");
                _logger.DebugFormat("Unable to get metadata from IMDSv2 endpoint. Falling back to IMDSv1 endpoint");

                // try IMDSv1 endpoint for metadata
                try
                {
                    string resp = DoRequest(metadata_base_url + "dynamic/instance-identity/document", HttpMethod.Get).Result;
                    dict = ParseMetadata(resp);
                }
                catch (Exception e2)
                {
                    _logger.Error(e2, "Exception occurred while getting EC2 metadata from IMDSv1");
                    _logger.DebugFormat("Failed to get EC2 instance metadata.");
                    return false;
                }
            }

            context = dict;
            return true;
        }

        private static async Task<string> DoRequest(string url, HttpMethod method, Dictionary<string, string> headers = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }

            HttpResponseMessage response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Unable to complete the request successfully");
            }
        }

        private static Dictionary<string, object> ParseMetadata(string jsonString)
        {
            JsonData data = JsonMapper.ToObject(jsonString);
            Dictionary<string, object> ec2_meta_dict = new Dictionary<string, object>();

            ec2_meta_dict.Add("instance_id", data["instanceId"]);
            ec2_meta_dict.Add("availability_zone", data["availabilityZone"]);
            ec2_meta_dict.Add("instance_size", data["instanceType"]);
            ec2_meta_dict.Add("ami_id", data["imageId"]);

            return ec2_meta_dict;
        }
    }
}
