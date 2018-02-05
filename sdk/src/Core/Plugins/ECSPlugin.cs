//-----------------------------------------------------------------------------
// <copyright file="ECSPlugin.cs" company="Amazon.com">
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
using Amazon.Runtime.Internal.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Amazon.XRay.Recorder.Core.Plugins
{
    /// <summary>
    /// This is a plugin for ECS.
    /// </summary>
    public class ECSPlugin : IPlugin
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(ECSPlugin));

        /// <summary>
        /// Gets the name of the origin associated with this plugin.
        /// </summary>
        /// <returns>The name of the origin associated with this plugin.</returns>
        public string Origin
        {
            get
            {
                return @"AWS::ECS::Container";
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
                return @"ecs";
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

            try
            {
                String hostname = Dns.GetHostName();

                if (hostname != null)
                {
                    dict.Add("container", hostname);
                }

                if (dict.Count == 0)
                {
                    _logger.DebugFormat("Failed to get runtime context for ECS.");
                    return false;
                }

                context = dict;
                return true;
            }
            catch (SocketException e)
            {
                _logger.DebugFormat("Could not get docker container ID from hostname.",e);
                return false;
            }
        }
    }
}
