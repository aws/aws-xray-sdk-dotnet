//-----------------------------------------------------------------------------
// <copyright file="AppSettings.netcore.cs" company="Amazon.com">
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
namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// This is a class for storing configuration of IConfiguration instance.
    /// </summary>
    public class XRayOptions
    {
        private string _pluginSetting;
        private string _samplingRuleManifest;
        private string _awsServiceHandlerManifest;
        private bool _isXRayTracingDisabled;
        private bool _useRuntimeErrors;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public XRayOptions()
        {
        }

        /// <summary>
        /// Creates instance of <see cref="XRayOptions"/>
        /// </summary>
        /// <param name="pluginSetting">Plugin setting.</param>
        /// <param name="samplingRuleManifest">Sampling rule file path</param>
        /// <param name="awsServiceHandlerManifest">AWS Service manifest file path.</param>
        /// <param name="isXRayTracingDisabled">Tracing disabled value, either true or false.</param>
        public XRayOptions(string pluginSetting, string samplingRuleManifest, string awsServiceHandlerManifest,
            bool isXRayTracingDisabled) : this(pluginSetting, samplingRuleManifest, awsServiceHandlerManifest, isXRayTracingDisabled, true)
        {
        }

        /// <summary>
        /// Creates instance of <see cref="XRayOptions"/>
        /// </summary>
        /// <param name="pluginSetting">Plugin setting.</param>
        /// <param name="samplingRuleManifest">Sampling rule file path</param>
        /// <param name="awsServiceHandlerManifest">AWS Service manifest file path.</param>
        /// <param name="isXRayTracingDisabled">Tracing disabled value, either true or false.</param>
        /// <param name="useRuntimeErrors">Should errors be thrown at runtime if segment not started, either true or false.</param>
        public XRayOptions(string pluginSetting, string samplingRuleManifest, string awsServiceHandlerManifest, bool isXRayTracingDisabled, bool useRuntimeErrors)
        {
            PluginSetting = pluginSetting;
            SamplingRuleManifest = samplingRuleManifest;
            AwsServiceHandlerManifest = awsServiceHandlerManifest;
            IsXRayTracingDisabled = isXRayTracingDisabled;
            UseRuntimeErrors = useRuntimeErrors;
        }

        /// <summary>
        /// Plugin setting.
        /// </summary>
        public string PluginSetting { get => _pluginSetting; set => _pluginSetting = value; }

        /// <summary>
        /// Sampling rule file path.
        /// </summary>
        public string SamplingRuleManifest { get => _samplingRuleManifest; set => _samplingRuleManifest = value; }

        /// <summary>
        /// AWS Service manifest file path.
        /// </summary>
        public string AwsServiceHandlerManifest { get => _awsServiceHandlerManifest; set => _awsServiceHandlerManifest = value; }

        /// <summary>
        /// Tracing disabled value, either true or false.
        /// </summary>
        public bool IsXRayTracingDisabled { get => _isXRayTracingDisabled; set => _isXRayTracingDisabled = value; }

        /// <summary>
        /// Determines whether runtime errors should occur when recording of data is attempted and a tracing header is not available, either true or false.
        /// </summary>
        public bool UseRuntimeErrors { get => _useRuntimeErrors; set => _useRuntimeErrors = value; }
    }
}
