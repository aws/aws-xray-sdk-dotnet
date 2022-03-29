//-----------------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="Amazon.com">
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

using Microsoft.Extensions.Configuration;
using System;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// Utility class to read <see cref="IConfiguration"/> object.
    /// </summary>
    public static class XRayConfiguration
    {
        private const string DEFAULT_CONFIG_SECTION = "XRay";
        private const string PluginSettingKey = "AWSXRayPlugins";
        private const string SamplingRuleManifestKey = "SamplingRuleManifest";
        private const string AWSServiceHandlerManifestKey = "AWSServiceHandlerManifest";
        private const string DisableXRayTracingKey = "DisableXRayTracing";
        private const string UseRuntimeErrorsKey = "UseRuntimeErrors";
        private const string CollectSqlQueries = "CollectSqlQueries";

        /// <summary>
        /// Reads configuration from <see cref="IConfiguration"/> object for X-Ray.
        /// </summary>
        /// <param name="config">Instance of <see cref="IConfiguration"/>.</param>
        /// <returns>Instance of <see cref="XRayOptions"/>.</returns>
        [CLSCompliant(false)]
        public static XRayOptions GetXRayOptions(this IConfiguration config)
        {
            return GetXRayOptions(config, DEFAULT_CONFIG_SECTION);
        }

        private static XRayOptions GetXRayOptions(IConfiguration config, string configSection)
        {
            var options = new XRayOptions();

            IConfiguration section;
            if (Object.Equals(config, null))
                return options;

            if (string.IsNullOrEmpty(configSection))
                section = config;
            else
                section = config.GetSection(configSection);

            if (section == null)
                return options;

            options.PluginSetting = GetSetting(PluginSettingKey, section);
            options.SamplingRuleManifest = GetSetting(SamplingRuleManifestKey, section);
            options.AwsServiceHandlerManifest =GetSetting(AWSServiceHandlerManifestKey, section);
            options.IsXRayTracingDisabled = GetSettingBool(DisableXRayTracingKey,section);
            options.UseRuntimeErrors = GetSettingBool(UseRuntimeErrorsKey, section, defaultValue: true);
            options.CollectSqlQueries = GetSettingBool(CollectSqlQueries, section, defaultValue: false);
            return options;
        }

        private static string GetSetting(string key, IConfiguration section)
        {
            if (!string.IsNullOrEmpty(section[key]))
            {
                return section[key];
            }
            else
                return null;
        }

        private static bool GetSettingBool(string key, IConfiguration section, bool defaultValue = false)
        {
            string value = GetSetting(key,section);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}
