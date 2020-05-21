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

using System.Collections.Generic;
using Amazon.Runtime.Internal.Util;
using Amazon.Util;

namespace Amazon.XRay.Recorder.Core.Plugins
{
    /// <summary>
    /// This is a plugin for EC2.
    /// </summary>
    public class EC2Plugin : IPlugin
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(EC2Plugin));

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
            if (EC2InstanceMetadata.InstanceId != null)
            {
                dict.Add("instance_id", EC2InstanceMetadata.InstanceId);
            }

            if (EC2InstanceMetadata.AvailabilityZone != null)
            {
                dict.Add("availability_zone", EC2InstanceMetadata.AvailabilityZone);
            }

            if (EC2InstanceMetadata.InstanceType != null)
            {
                dict.Add("instance_size", EC2InstanceMetadata.InstanceType);
            }

            if (EC2InstanceMetadata.AmiId != null)
            {
                dict.Add("ami_id", EC2InstanceMetadata.AmiId);
            }

            if (dict.Count == 0)
            {
                _logger.DebugFormat("Unable to contact EC2 metadata service, failed to get runtime context.");
                return false;
            }

            context = dict;
            return true;
        }
    }
}
