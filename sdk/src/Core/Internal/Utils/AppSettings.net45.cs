//-----------------------------------------------------------------------------
// <copyright file="AppSettings.cs" company="Amazon.com">
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

using System.Configuration;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// This is a wrapper class for useful app settings in app.config and web.config
    /// </summary>
    public static class AppSettings
    {
        private const string PluginSettingKey = "AWSXRayPlugins";
        private const string SamplingRuleManifestKey = "SamplingRuleManifest";
        private const string AWSServiceHandlerManifestKey = "AWSServiceHandlerManifest";
        private const string DisableXRayTracingKey = "DisableXRayTracing";
        private const string UseRuntimeErrorsKey = "UseRuntimeErrors";

        private static string _pluginSetting = GetSetting(PluginSettingKey);
        private static string _samplingRuleManifest = GetSetting(SamplingRuleManifestKey);
        private static string _awsServiceHandlerManifest = GetSetting(AWSServiceHandlerManifestKey);
        private static bool _isXRayTracingDisabled = GetSettingBool(DisableXRayTracingKey);
        private static bool _useRuntimeErrors = GetSettingBool(UseRuntimeErrorsKey);

        /// <summary>
        /// Gets the plugin setting from app settings
        /// </summary>
        public static string PluginSetting
        {
            get
            {
                return _pluginSetting;
            }
        }

        /// <summary>
        /// Gets the sampling rule manifest path from app settings
        /// </summary>
        public static string SamplingRuleManifest
        {
            get
            {
                return _samplingRuleManifest;
            }
        }

        /// <summary>
        /// Gets the aws service info manifest
        /// </summary>
        public static string AWSServiceHandlerManifest
        {
            get
            {
                return _awsServiceHandlerManifest;
            }
        }

        /// <summary>
        /// Gets a value indicating whether X-Ray tracing is disabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if X-Ray tracing is disabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsXRayTracingDisabled
        {
            get
            {
                return _isXRayTracingDisabled;
            }
        }

        /// <summary>
        /// Gets context missing strategy setting from the app setting.
        /// </summary>
        public static bool UseRuntimeErrors { get => _useRuntimeErrors;}

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public static void Reset()
        {
            _pluginSetting = GetSetting(PluginSettingKey);
            _samplingRuleManifest = GetSetting(SamplingRuleManifestKey);
            _awsServiceHandlerManifest = GetSetting(AWSServiceHandlerManifestKey);
            _isXRayTracingDisabled = GetSettingBool(DisableXRayTracingKey);
            _useRuntimeErrors = GetSettingBoolForRuntimeError(UseRuntimeErrorsKey);
        }

        private static string GetSetting(string key)
        {
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings == null)
            {
                return null;
            }

            string value = appSettings[key];
            return value;
        }

        private static bool GetSettingBool(string key, bool defaultValue = false)
        {
            string value = GetSetting(key);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }

            return defaultValue;
        }

        // If the key not present, default value set to true.
        private static bool GetSettingBoolForRuntimeError(string key, bool defaultValue = true)
        {
            string value = GetSetting(key);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}
