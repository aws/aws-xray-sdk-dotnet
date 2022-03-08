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
        const string metadata_base_url = "http://169.254.169.254/latest/";

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
        /// <param name="context">When the method returns, contains the runtime context of the plugin.</param>
        /// <returns>true if the runtime context is available; Otherwise, false.</returns>
        public bool TryGetRuntimeContext(out IDictionary<string, object> context)
        {
            // get the token
            string token = GetToken();

            // get the metadata
            context = GetMetadata(token);

            if (context.Count == 0)
            {
                _logger.DebugFormat("Could not get instance metadata");
                return false;
            }

            return true;
        }

        private string GetToken()
        {
            string token = null;
            try
            {
                Dictionary<string, string> header = new Dictionary<string, string>(1);
                header.Add("X-aws-ec2-metadata-token-ttl-seconds", "60");
                token = DoRequest(metadata_base_url + "api/token", HttpMethod.Put, header);
            }
            catch (Exception)
            {
                _logger.DebugFormat("Failed to get token for IMDSv2");
            }

            return token;
        }


        private IDictionary<string, object> GetMetadata(string token)
        {
            try
            {
                Dictionary<string, string> headers = null;
                if (token != null)
                {
                    headers = new Dictionary<string, string>(1);
                    headers.Add("X-aws-ec2-metadata-token", token);
                }
                string identity_doc_url = metadata_base_url + "dynamic/instance-identity/document";
                string doc_string = DoRequest(identity_doc_url, HttpMethod.Get, headers);
                return ParseMetadata(doc_string);
            }
            catch (Exception)
            {
                _logger.DebugFormat("Error occurred while getting EC2 metadata");
                return new Dictionary<string, object>();
            }
        }


        protected virtual string DoRequest(string url, HttpMethod method, Dictionary<string, string> headers = null)
        {
            var httpWebRequest = WebRequest.CreateHttp(url);

            httpWebRequest.Timeout = 2000; // 2 seconds timeout
            httpWebRequest.Method = method.Method;

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    httpWebRequest.Headers.Add(item.Key, item.Value);
                }
            }

            using(var response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                if (response.StatusCode < HttpStatusCode.OK || (int)response.StatusCode > 299)
                {
                    throw new Exception("Unable to complete the request successfully");
                }

                var encoding = Encoding.GetEncoding(response.ContentEncoding);

                using (var streamReader = new StreamReader(response.GetResponseStream(), encoding))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }


        private IDictionary<string, object> ParseMetadata(string jsonString)
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
