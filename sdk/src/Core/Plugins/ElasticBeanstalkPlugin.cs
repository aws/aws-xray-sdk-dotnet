//-----------------------------------------------------------------------------
// <copyright file="ElasticBeanstalkPlugin.cs" company="Amazon.com">
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
using System.IO;
using ThirdParty.Json.LitJson;

namespace Amazon.XRay.Recorder.Core.Plugins
{
    /// <summary>
    /// This is a plugin for Elastic Beanstalk.
    /// </summary>
    public class ElasticBeanstalkPlugin : IPlugin
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(ElasticBeanstalkPlugin));
        private const String _confPath = " C:\\Program Files\\Amazon\\XRay\\environment.conf";

        /// <summary>
        /// Gets the name of the origin associated with this plugin.
        /// </summary>
        /// <returns>The name of the origin associated with this plugin.</returns>
        public string Origin
        {
            get
            {
                return @"AWS::ElasticBeanstalk::Environment";
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
                return @"elastic_beanstalk";
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

            var dict = ElasticBeanstalkPlugin.GetElasticBeanstalkMetaData();

            if (dict.Count == 0)
            {
                _logger.DebugFormat("Failed to get meta data for Elastic Beanstalk.");
                return false;
            }

            context = dict;

            return true;
        }

        private static Dictionary<string, object> GetElasticBeanstalkMetaData()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            try
            {
                using (Stream stream = new FileStream(_confPath, FileMode.Open, FileAccess.Read))
                {
                    dictionary = ElasticBeanstalkPlugin.ReadStream(stream);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to access Elastic Beanstalk configuration file.");
            }

            return dictionary;
        }

        private static Dictionary<string, object> ReadStream(Stream stream)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            using (var reader = new StreamReader(stream))
            {
                try
                {
                    dict = JsonMapper.ToObject<Dictionary<string, object>>(reader);
                }
                catch (JsonException e)
                {
                    _logger.Error(e, "Failed to load Elastic Beanstalk configuration file.");
                }
            }

            return dict;
        }
    }
}
