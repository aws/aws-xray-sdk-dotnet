//-----------------------------------------------------------------------------
// <copyright file="AWSXRayRecorderBuilder.cs" company="Amazon.com">
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
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Plugins;
using Amazon.XRay.Recorder.Core.Sampling;
using Amazon.XRay.Recorder.Core.Strategies;

namespace Amazon.XRay.Recorder.Core
{
    /// <summary>
    /// This class provides utilities to build an instance of <see cref="AWSXRayRecorder"/> with different configurations.
    /// </summary>
    public class AWSXRayRecorderBuilder
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorderBuilder));
        private static readonly char[] _validSeparators = { ' ', ',', ';' };
        private static readonly string _pluginNamespace = typeof(IPlugin).Namespace;

        private readonly List<IPlugin> _plugins = new List<IPlugin>();
        private ISamplingStrategy _samplingStrategy;
        private ContextMissingStrategy _contextMissingStrategy = ContextMissingStrategy.RUNTIME_ERROR;
        private ISegmentEmitter _segmentEmitter;
        private string _daemonAddress;
        private ITraceContext _traceContext;
        private ExceptionSerializationStrategy _exceptionSerializationStrategy;
        private IStreamingStrategy _streamingStrategy;

        /// <summary>
        /// Gets a read-only copy of current plugins in the builder
        /// </summary>
        public IReadOnlyList<IPlugin> Plugins
        {
            get
            {
                return _plugins.AsReadOnly();
            }
        }

#if NET45
        /// <summary>
        /// Initializes <see cref="AWSXRayRecorderBuilder"/> instance with default settings.
        /// </summary>
        /// <returns>instance of <see cref="AWSXRayRecorderBuilder"/></returns>
        public static AWSXRayRecorderBuilder GetDefaultBuilder()
        {
            return new AWSXRayRecorderBuilder().WithPluginsFromAppSettings().WithContextMissingStrategyFromAppSettings();
        }
#endif

#if NET45
        /// <summary>
        /// Reads plugin settings from app settings, and adds new instance of each plugin into the builder.
        /// If the plugin settings doesn't exist or the value of the settings is invalid, nothing will be added.
        /// </summary>
        /// <returns>The builder with plugin added.</returns>
        public AWSXRayRecorderBuilder WithPluginsFromAppSettings()
        {
            var setting = AppSettings.PluginSetting;
            if (string.IsNullOrEmpty(setting))
            {
                _logger.DebugFormat("Plugin setting is missing.");
                return this;
            }

            PopulatePlugins(setting);

            return this;
        }

        /// <summary>
        /// Reads useRuntimeErrors settings from app settings, and adds into the builder.
        /// If the useRuntimeErrors settings doesn't exist, it defaults to true and ContextMissingStrategy.RUNTIME_ERROR is used.
        /// </summary>
        /// <returns>The builder with context missing strategy set.</returns>
        public AWSXRayRecorderBuilder WithContextMissingStrategyFromAppSettings()
        {
            bool useRuntimeErrors = AppSettings.UseRuntimeErrors;
            if (useRuntimeErrors)
            {
                return WithContextMissingStrategy(ContextMissingStrategy.RUNTIME_ERROR);
            }
            return WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR);
        }
#else
        /// <summary>
        /// Builds <see cref="AWSXRayRecorderBuilder"/> instance with xrayoptions.
        /// </summary>
        /// <param name="xRayOptions">Instance of <see cref="XRayOptions"/></param>
        /// <returns>Instance of <see cref="AWSXRayRecorderBuilder"/>.</returns>
        public AWSXRayRecorderBuilder WithPluginsFromConfig(XRayOptions xRayOptions)
        {
            var setting = xRayOptions.PluginSetting;
            if (string.IsNullOrEmpty(setting))
            {
                _logger.DebugFormat("Plugin setting is missing.");
                return this;
            }

            PopulatePlugins(setting);

            return this;
        }

        /// <summary>
        /// Reads useRuntimeErrors settings from config instance, and adds into the builder.
        /// If the useRuntimeErrors settings doesn't exist, it defaults to true and ContextMissingStrategy.RUNTIME_ERROR is used.
        /// </summary>
        /// <returns>The builder with context missing strategy set.</returns>
        public AWSXRayRecorderBuilder WithContextMissingStrategyFromConfig(XRayOptions xRayOptions)
        {
            if (xRayOptions.UseRuntimeErrors)
            {
                return WithContextMissingStrategy(ContextMissingStrategy.RUNTIME_ERROR);
            }
            else
            {
                return WithContextMissingStrategy(ContextMissingStrategy.LOG_ERROR);
            }
        }

        /// <summary>
        /// Build a instance of <see cref="AWSXRayRecorder"/> with <see cref="XRayOptions"/> class.
        /// <param name="xRayOptions">An instance of <see cref="XRayOptions"/> class.</param>
        /// </summary>
        /// <returns>A new instance of <see cref="AWSXRayRecorder"/> class.</returns>
        public AWSXRayRecorder Build(XRayOptions xRayOptions)
        {
            var recorder = new AWSXRayRecorder(xRayOptions);

            PopulateRecorder(recorder);

            return recorder;
        }

        /// <summary>
        /// Configures a instance of <see cref="AWSXRayRecorder"/> with existing configuration added to the builder.
        /// </summary>
        /// <param name="recorder">An instance of <see cref="AWSXRayRecorder"/>.</param>
        /// <returns>A new instance of <see cref="AWSXRayRecorder"/>.</returns>

        public AWSXRayRecorder Build(AWSXRayRecorder recorder)
        {
            PopulateRecorder(recorder);

            return recorder;
        }
#endif
        /// <summary>
        /// Adds the given plugin to builder
        /// </summary>
        /// <param name="plugin">A specific plugin to add.</param>
        /// <returns>The builder with plugin added.</returns>
        public AWSXRayRecorderBuilder WithPlugin(IPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            _plugins.Add(plugin);
            return this;
        }

        /// <summary>
        /// Sets the address for the xray daemon.
        /// </summary>
        /// <param name="address">The xray daemon address.</param>
        /// <returns>The builder with the specified xray daemon address.</returns>
        public AWSXRayRecorderBuilder WithDaemonAddress(String address)
        {
            if (String.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException("DaemonAddress");
            }

            _daemonAddress = address;
            return this;
        }

        /// <summary>
        /// Adds the given sampling strategy to builder. There can exist only one sampling strategy.
        /// Any previous value of sampling strategy will be overwritten.
        /// </summary>
        /// <param name="newStrategy">A sampling strategy to add</param>
        /// <returns>The builder with sampling strategy added.</returns>
        public AWSXRayRecorderBuilder WithSamplingStrategy(ISamplingStrategy newStrategy)
        {
            _samplingStrategy = newStrategy ?? throw new ArgumentNullException("SamplingStrategy");
            return this;
        }

        /// <summary>
        /// Adds the given streaming strategy to builder. There can exist only one streaming strategy.
        /// Any previous value of streaming strategy will be overwritten.
        /// </summary>
        /// <param name="newStreamingStrategy">A streaming strategy to add</param>
        /// <returns>The builder with streaming strategy added</returns>
        public AWSXRayRecorderBuilder WithStreamingStrategy(IStreamingStrategy newStreamingStrategy)
        {
            _streamingStrategy = newStreamingStrategy ?? throw new ArgumentNullException("StreamingStrategy");
            return this;
        }

        /// <summary>
        /// Adds the context missing strategy.
        /// </summary>
        /// <param name="strategy">The ContextMissingStrategy.</param>
        /// <returns>The builder with context missing strategy added.</returns>
        public AWSXRayRecorderBuilder WithContextMissingStrategy(ContextMissingStrategy strategy)
        {
            _contextMissingStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Adds the provided <see cref="ISegmentEmitter"/> instance.
        /// </summary>
        /// <param name="segmentEmitter">The provided <see cref="ISegmentEmitter"/> instance.</param>
        /// <returns>The builder with ISegmentEmitter added.</returns>
        public AWSXRayRecorderBuilder WithSegmentEmitter(ISegmentEmitter segmentEmitter)
        {
            _segmentEmitter = segmentEmitter ?? throw new ArgumentNullException("SegmentEmitter");
            return this;
        }

        /// <summary>
        /// Configures recorder with <see cref="ITraceContext"/> instance.
        /// </summary>
        /// <param name="traceContext">The provided <see cref="ITraceContext"/> instance.</param>
        /// <returns>The builder with ITraceContext added.</returns>
        public AWSXRayRecorderBuilder WithTraceContext(ITraceContext traceContext)
        {
            _traceContext = traceContext ?? throw new ArgumentNullException("TraceContext");
            return this;
        }

        /// <summary>
        /// Configures recorder with provided <see cref="ExceptionSerializationStrategy"/>. While setting number consider max trace size
        /// limit : https://aws.amazon.com/xray/pricing/
        /// </summary>
        /// <param name="exceptionSerializationStartegy">An instance of <see cref="ExceptionSerializationStrategy"/></param>
        /// <returns>The builder with exception serialization strategy added.</returns>
        public AWSXRayRecorderBuilder WithExceptionSerializationStrategy(ExceptionSerializationStrategy exceptionSerializationStartegy)
        {
            _exceptionSerializationStrategy = exceptionSerializationStartegy ?? throw new ArgumentNullException("ExceptionSerializationStartegy"); ;
            return this;
        }

        /// <summary>
        /// Build a instance of <see cref="AWSXRayRecorder"/> with existing configuration added to the builder.
        /// </summary>
        /// <returns>A new instance of <see cref="AWSXRayRecorder"/>.</returns>
        public AWSXRayRecorder Build()
        {
            var recorder = new AWSXRayRecorder();

            PopulateRecorder(recorder);
            
            return recorder;
        }

        private void PopulatePlugins(string setting)
        {
            var pluginSettings = setting.Split(_validSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pluginSetting in pluginSettings)
            {
                string fullTypeName = _pluginNamespace + "." + pluginSetting;
                var type = Type.GetType(fullTypeName);
                if (type == null)
                {
                    _logger.DebugFormat("Invalid plugin setting: {0}", pluginSetting);
                    continue;
                }

                try
                {
                    var plugin = Activator.CreateInstance(type) as IPlugin;
                    if (plugin == null)
                    {
                        _logger.DebugFormat("Failed to create an instance of type: {0}", type.FullName);
                        continue;
                    }

                    _plugins.Add(plugin);
                }
                catch (MissingMethodException e)
                {
                    _logger.Debug(e, "Failed to create the plugin: {0}", type.FullName);
                }
            }
        }

        private void PopulateRecorder(AWSXRayRecorder recorder)
        {
            foreach (IPlugin plugin in _plugins)
            {
                IDictionary<string, object> pluginContext;
                if (plugin.TryGetRuntimeContext(out pluginContext))
                {
                    recorder.RuntimeContext.Add(plugin.ServiceName, pluginContext);
                    recorder.Origin = plugin.Origin;
                }
            }      

            recorder.ContextMissingStrategy = _contextMissingStrategy;

            if (_segmentEmitter != null)
            {
                recorder.Emitter = _segmentEmitter;
            }

            if (_samplingStrategy != null)
            {
                recorder.SamplingStrategy = _samplingStrategy;
            }

            if (_streamingStrategy != null)
            {
                recorder.StreamingStrategy = _streamingStrategy;
            }

            if (_daemonAddress != null) 
            {
                recorder.SetDaemonAddress(_daemonAddress);
            }

            if (_traceContext != null)
            {
                recorder.SetTraceContext(_traceContext);
            }

            if (_exceptionSerializationStrategy != null)
            {
                recorder.SetExceptionSerializationStrategy(_exceptionSerializationStrategy);
            }
        }
    }
}
