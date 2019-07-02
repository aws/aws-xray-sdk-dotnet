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
        /// <param name="collectSqlQueries">
        /// Include the TraceableSqlCommand.CommandText in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are including sensitive information as clear text.
        /// This flag can also be overridden for each TraceableSqlCommand instance individually.
        /// See the official documentation on <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters?view=netframework-4.7.2">SqlCommand.Parameters</a>
        /// </param>
        public XRayOptions(string pluginSetting, string samplingRuleManifest, string awsServiceHandlerManifest, bool isXRayTracingDisabled, bool useRuntimeErrors, bool collectSqlQueries = false)
        {
            PluginSetting = pluginSetting;
            SamplingRuleManifest = samplingRuleManifest;
            AwsServiceHandlerManifest = awsServiceHandlerManifest;
            IsXRayTracingDisabled = isXRayTracingDisabled;
            UseRuntimeErrors = useRuntimeErrors;
            CollectSqlQueries = collectSqlQueries;
        }

        /// <summary>
        /// Plugin setting.
        /// </summary>
        public string PluginSetting { get; set; }

        /// <summary>
        /// Sampling rule file path.
        /// </summary>
        public string SamplingRuleManifest { get; set; }

        /// <summary>
        /// AWS Service manifest file path.
        /// </summary>
        public string AwsServiceHandlerManifest { get; set; }

        /// <summary>
        /// Tracing disabled value, either true or false.
        /// </summary>
        public bool IsXRayTracingDisabled { get; set; }

        /// <summary>
        /// For missing Segments/Subsegments, if set to true, runtime exception is thrown, if set to false, runtime exceptions are avoided and logged.
        /// </summary>
        public bool UseRuntimeErrors { get; set; } = true;

        /// <summary>
        /// Include the TraceableSqlCommand.CommandText in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are not including sensitive information as clear text.
        /// When set to true, the sanitized sql query will be recorded for all the instances of TraceableSqlCommand
        /// in the application, unless it is overridden on the individual TraceableSqlCommand instances.
        /// </summary>
        public bool CollectSqlQueries { get; set; } = false;
    }
}
