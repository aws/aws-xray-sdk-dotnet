//-----------------------------------------------------------------------------
// <copyright file="AWSSDKHandler.cs" company="Amazon.com">
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
using Amazon.Runtime.Internal;
using Amazon.XRay.Recorder.Handlers.AwsSdk.Internal;
using System;
using System.IO;

namespace Amazon.XRay.Recorder.Handlers.AwsSdk
{
    /// <summary>
    /// The AWS SDK handler to register X-Ray with <see cref="Runtime.AmazonServiceClient"/> which can intercept downstream requests and responses.
    /// </summary>
    public static class AWSSDKHandler
    {
        private static XRayPipelineCustomizer _customizer;

        /// <summary>
        /// Registers X-Ray for all instances of <see cref="Runtime.AmazonServiceClient"/>.
        /// </summary>
        public static void RegisterXRayForAllServices()
        {
            _customizer = GetCustomizer();
            _customizer.RegisterAll = true;
        }

        /// <summary>
        /// Registers X-Ray for all instances of <see cref="Runtime.AmazonServiceClient"/> with a given custom AWS Service Manifest File.
        /// </summary>
        /// <param name="path"> Absolute path to the file which contains the operation parameter whitelist configuration.</param>
        public static void RegisterXRayForAllServices(String path)
        {
            _customizer = GetCustomizer();
            _customizer.RegisterAll = true;
            _customizer.AWSServiceHandlerManifest = XRayPipelineHandler.GetAWSServiceManifest(path);
        }

        /// <summary>
        /// Registers X-Ray for the given type of <see cref="Runtime.AmazonServiceClient"/>.
        /// </summary>
        public static void RegisterXRay<T>()
        {
            _customizer = GetCustomizer();
            _customizer.AddType(typeof(T));
        }

        /// <summary>
        /// Registers file path of AWS Service Manifest file. This file would be used for all the registered <see cref="Runtime.AmazonServiceClient"/> instances. 
        /// </summary>
        /// <param name="path"> Absolute path to the file which contains the operation parameter whitelist configuration.</param>
        public static void RegisterXRayManifest(String path)
        {
            _customizer = GetCustomizer();
            _customizer.AWSServiceHandlerManifest = XRayPipelineHandler.GetAWSServiceManifest(path);
        }

        /// <summary>
        /// Registers AWS Service Manifest resource stream. This stream would be used for all the registered <see cref="Runtime.AmazonServiceClient"/> instances. 
        /// </summary>
        /// <param name="stream"> stream for manifest which contains the operation parameter whitelist configuration.</param>
        public static void RegisterXRayManifest(Stream stream)
        {
            _customizer = GetCustomizer();
            _customizer.AWSServiceHandlerManifest = XRayPipelineHandler.GetAWSServiceManifest(stream);
        }

        private static XRayPipelineCustomizer GetCustomizer()
        {
            if (_customizer == null)
            {
                _customizer = new XRayPipelineCustomizer();
                RuntimePipelineCustomizerRegistry.Instance.Register(_customizer);
            }

            return _customizer;
        }
    }
}
