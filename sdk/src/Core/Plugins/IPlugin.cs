//-----------------------------------------------------------------------------
// <copyright file="IPlugin.cs" company="Amazon.com">
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

namespace Amazon.XRay.Recorder.Core.Plugins
{
    /// <summary>
    /// Interface for plugin which collect information of runtime
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of the origin associated with this plugin. By default, <see cref="AWSXRayRecorder"/> will infect segment with
        /// the origin name of last loaded plugin.
        /// </summary>
        string Origin { get; }

        /// <summary>
        /// Gets the name of the service associated with this plugin.
        /// </summary>
        /// <returns>The name of the service that this plugin is associated with.</returns>
        string ServiceName { get; }

        /// <summary>
        /// Gets the context of the runtime that this plugin is associated with.
        /// </summary>
        /// <param name="context">When the method returns, contains the runtime context of the plugin, or null if the runtime context is not available.</param>
        /// <returns>true if the runtime context is available; Otherwise, false.</returns>
        bool TryGetRuntimeContext(out IDictionary<string, object> context);
    }
}
