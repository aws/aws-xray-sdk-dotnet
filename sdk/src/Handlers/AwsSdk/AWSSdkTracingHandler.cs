//-----------------------------------------------------------------------------
// <copyright file="AWSSdkTracingHandler.cs" company="Amazon.com">
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.AwsSdk.Entities;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.Handlers.AwsSdk
{
    /// <summary>
    /// The event handler to register with AmazonServiceClient which can intercept downstream requests and responses.
    /// </summary>
    [Obsolete("AWSSdkTracingHandler is deprecated, please use AWSSDKHandler instead.")]
    public class AWSSdkTracingHandler
    {
        private const string DefaultAwsWhitelistManifestResourceName = "Amazon.XRay.Recorder.Handlers.AwsSdk.DefaultAWSWhitelist.json";
        private static readonly Logger _logger = Logger.GetLogger(typeof(AWSXRayRecorder));

        private AWSXRayRecorder _recorder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AWSSdkTracingHandler" /> class.
        /// </summary>
        /// <param name="recorder">An instance of <see cref="AWSXRayRecorder" /> used to start and stop segment</param>
        /// <exception cref="System.ArgumentNullException">Thrown when recorder is null.</exception>
        public AWSSdkTracingHandler(AWSXRayRecorder recorder)
        {
            if (recorder == null)
            {
                throw new ArgumentNullException("recorder");
            }

            InitWithDefaultAWSWhitelist(recorder);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="AWSSdkTracingHandler" /> class.
        /// </summary>
        /// <param name="recorder">An instance of <see cref="AWSXRayRecorder" /> used to start and stop segment</param>
        /// <param name="path">Path to the file which contains the operation parameter whitelist configuration.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when recorder is null.</exception>
        public AWSSdkTracingHandler(AWSXRayRecorder recorder, string path)
        {
            if (recorder == null)
            {
                throw new ArgumentNullException("recorder");
            }

            if (string.IsNullOrEmpty(path))
            {
                _logger.DebugFormat("The path is null or empty, initializing with default AWS whitelist.");
                InitWithDefaultAWSWhitelist(recorder);
            }
            else
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    Init(recorder, stream);
                }
            }
        }

        /// <summary>
        /// Gets AWS service manifest of operation parameter whitelist.
        /// </summary>
        public AWSServiceHandlerManifest AWSServiceHandlerManifest { get; private set; }

        /// <summary>
        /// Add tracing event handler to a given AmazonServiceClient.
        /// </summary>
        /// <param name="serviceClient">The target AmazonServiceClient</param>
        /// <exception cref="System.ArgumentNullException">Thrown when serviceClient is null.</exception>
        public void AddEventHandler(AmazonServiceClient serviceClient)
        {
            if (serviceClient == null)
            {
                throw new ArgumentNullException("serviceClient");
            }

            serviceClient.BeforeRequestEvent += BeforeRequestEventHandler;
            serviceClient.AfterResponseEvent += AfterResponseEventHandler;
            serviceClient.ExceptionEvent += ExceptionEventHandler;
        }

        private static bool TryReadPropertyValue(object obj, string propertyName, out object value)
        {
            value = 0;
            try
            {
                if (obj == null || propertyName == null)
                {
                    return false;
                }

                var property = obj.GetType().GetProperty(propertyName);
                if (property == null)
                {
                    _logger.DebugFormat("Property doesn't exist. {0}", propertyName);
                    return false;
                }

                value = property.GetValue(obj);
                return true;
            }
            catch (ArgumentNullException e)
            {
                _logger.Error(e, "Failed to read property because argument is null.");
                return false;
            }
            catch (AmbiguousMatchException e)
            {
                _logger.Error(e, "Failed to read property because of duplicate property name.");
                return false;
            }
        }

        /// <summary>
        /// Removes amazon prefix from service name. There are two type of service name.
        ///     Amazon.DynamoDbV2
        ///     AmazonS3
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns>String after removed Amazon prefix.</returns>
        private static string RemoveAmazonPrefixFromServiceName(string serviceName)
        {
            return RemovePrefix(RemovePrefix(serviceName, "Amazon"), ".");
        }

        private static string RemovePrefix(string originalString, string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException("prefix");
            }

            if (originalString == null)
            {
                throw new ArgumentNullException("originalString");
            }

            if (originalString.StartsWith(prefix))
            {
                return originalString.Substring(prefix.Length);
            }

            return originalString;
        }

        private static string RemoveSuffix(string originalString, string suffix)
        {
            if (suffix == null)
            {
                throw new ArgumentNullException("suffix");
            }

            if (originalString == null)
            {
                throw new ArgumentNullException("originalString");
            }

            if (originalString.EndsWith(suffix))
            {
                return originalString.Substring(0, originalString.Length - suffix.Length);
            }

            return originalString;
        }

        private static void AddMapProperty(IDictionary<string, object> aws, object obj, string propertyName, string renameTo = null)
        {
            if (!TryReadPropertyValue(obj, propertyName, out object propertyValue))
            {
                _logger.DebugFormat("Failed to read property value: {0}", propertyName);
                return;
            }

            var dictionaryValue = propertyValue as IDictionary;
            if (dictionaryValue == null)
            {
                _logger.DebugFormat("Property value does not implements IDictionary: {0}", propertyName);
                return;
            }

            var newPropertyName = string.IsNullOrEmpty(renameTo) ? propertyName : renameTo;
            aws[newPropertyName.FromCamelCaseToSnakeCase()] = dictionaryValue.Keys;
        }

        private static void AddListProperty(IDictionary<string, object> aws, object obj, string propertyName, string renameTo = null)
        {
            if (!TryReadPropertyValue(obj, propertyName, out object propertyValue))
            {
                _logger.DebugFormat("Failed to read property value: {0}", propertyName);
                return;
            }

            var listValue = propertyValue as IList;
            if (listValue == null)
            {
                _logger.DebugFormat("Property value does not implements IList: {0}", propertyName);
                return;
            }

            var newPropertyName = string.IsNullOrEmpty(renameTo) ? propertyName : renameTo;
            aws[newPropertyName.FromCamelCaseToSnakeCase()] = listValue.Count;
        }

        private void InitWithDefaultAWSWhitelist(Amazon.XRay.Recorder.Core.AWSXRayRecorder recorder)
        {

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultAwsWhitelistManifestResourceName))
            {
                Init(recorder, stream);
            }
        }

        private void Init(Amazon.XRay.Recorder.Core.AWSXRayRecorder recorder, Stream stream)
        {
            _recorder = recorder;

            using (var reader = new StreamReader(stream))
            {
                try
                {
                    AWSServiceHandlerManifest = JsonMapper.ToObject<AWSServiceHandlerManifest>(reader);
                }
                catch (JsonException e)
                {
                    _logger.Error(e, "Failed to load AWSServiceHandlerManifest.");
                }
            }
        }

        private void BeforeRequestEventHandler(object sender, RequestEventArgs e)
        {
            var args = e as WebServiceRequestEventArgs;
            if (args == null)
            {
                _logger.DebugFormat("Failed to handle BeforeRequestEvent, because e can't be converted to WebServiceRequestEventArgs.");
                return;
            }

            if (TraceHeader.TryParse(TraceContext.GetEntity(), out TraceHeader traceHeader))
            {
                args.Headers[TraceHeader.HeaderKey] = traceHeader.ToString();
            }
            else
            {
                _logger.DebugFormat("Failed to inject trace header to AWS SDK request as the segment can't be converted to TraceHeader.");
            }
            _recorder.BeginSubsegment(RemoveAmazonPrefixFromServiceName(args.ServiceName));
            _recorder.SetNamespace("aws");
        }

        private void AfterResponseEventHandler(object sender, ResponseEventArgs e)
        {
            if (AppSettings.IsXRayTracingDisabled)
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not handle AWSSDK response.");
                return;
            }

            var subsegment = TraceContext.GetEntity();
            var args = e as WebServiceResponseEventArgs;
            if (args == null)
            {
                _logger.DebugFormat("Failed to handle AfterResponseEvent, because e can't be converted to WebServiceResponseEventArgs.");
                return;
            }

            var client = sender as AmazonServiceClient;
            if (client == null)
            {
                _logger.DebugFormat("Failed to handle AfterResponseEvent, because sender can't be converted to AmazonServiceClient.");
                return;
            }

            var serviceName = RemoveAmazonPrefixFromServiceName(args.ServiceName);
            var operation = RemoveSuffix(args.Request.GetType().Name, "Request");

            subsegment.Aws["region"] = client.Config.RegionEndpoint?.SystemName;
            subsegment.Aws["operation"] = operation;
            subsegment.Aws["request_id"] = args.Response.ResponseMetadata.RequestId;

            AddRequestSpecificInformation(serviceName, operation, args.Request, subsegment.Aws);
            AddResponseSpecificInformation(serviceName, operation, args.Response, subsegment.Aws);

            _recorder.EndSubsegment();
        }

        private void ExceptionEventHandler(object sender, ExceptionEventArgs e)
        {
            if (AppSettings.IsXRayTracingDisabled)
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not handle AWSSDK exception.");
                return;
            }

            var subsegment = TraceContext.GetEntity();
            var args = e as WebServiceExceptionEventArgs;

            if (args == null)
            {
                _logger.DebugFormat("Failed to handle ExceptionEvent, because e can't be converted to WebServiceExceptionEventArgs.");
                return;
            }

            var client = sender as AmazonServiceClient;
            if (client == null)
            {
                _logger.DebugFormat("Failed to handle ExceptionEvent, because sender can't be converted to AmazonServiceClient.");
                return;
            }

            var serviceName = RemoveAmazonPrefixFromServiceName(args.ServiceName);
            var operation = RemoveSuffix(args.Request.GetType().Name, "Request");

            subsegment.Aws["region"] = client.Config.RegionEndpoint?.SystemName;
            subsegment.Aws["operation"] = operation;
            if (args.Headers.TryGetValue("x-amzn-RequestId", out string requestId))
            {
                subsegment.Aws["request_id"] = requestId;
            }

            AddRequestSpecificInformation(serviceName, operation, args.Request, subsegment.Aws);

            subsegment.AddException(args.Exception);
            _recorder.EndSubsegment();
        }

        private void AddRequestSpecificInformation(string serviceName, string operation, AmazonWebServiceRequest request, IDictionary<string, object> aws)
        {
            if (serviceName == null)
            {
                throw new ArgumentNullException("serviceName");
            }

            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (aws == null)
            {
                throw new ArgumentNullException("aws");
            }

            if (AWSServiceHandlerManifest == null)
            {
                _logger.DebugFormat("AWSServiceHandlerManifest doesn't exist.");
                return;
            }

            if (!AWSServiceHandlerManifest.Services.TryGetValue(serviceName, out AWSServiceHandler serviceHandler))
            {
                _logger.DebugFormat("Service name doesn't exist in AWSServiceHandlerManifest: serviceName = {0}.", serviceName);
                return;
            }

            if (!serviceHandler.Operations.TryGetValue(operation, out AWSOperationHandler operationHandler))
            {
                _logger.DebugFormat("Operation doesn't exist in AwsServiceInfo: serviceName = {0}, operation = {1}.", serviceName, operation);
                return;
            }

            if (operationHandler.RequestParameters != null)
            {
                foreach (string parameter in operationHandler.RequestParameters)
                {
                    if (TryReadPropertyValue(request, parameter, out object propertyValue))
                    {
                        aws[parameter.FromCamelCaseToSnakeCase()] = propertyValue;
                    }
                }
            }

            if (operationHandler.RequestDescriptors != null)
            {
                foreach (KeyValuePair<string, AWSOperationRequestDescriptor> kv in operationHandler.RequestDescriptors)
                {
                    var propertyName = kv.Key;
                    var descriptor = kv.Value;

                    if (descriptor.Map && descriptor.GetKeys)
                    {
                        AWSSdkTracingHandler.AddMapProperty(aws, request, propertyName, descriptor.RenameTo);
                    }
                    else if (descriptor.List && descriptor.GetCount)
                    {
                        AWSSdkTracingHandler.AddListProperty(aws, request, propertyName, descriptor.RenameTo);
                    }
                }
            }
        }

        private void AddResponseSpecificInformation(string serviceName, string operation, AmazonWebServiceResponse response, IDictionary<string, object> aws)
        {
            if (serviceName == null)
            {
                throw new ArgumentNullException("serviceName");
            }

            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            if (aws == null)
            {
                throw new ArgumentNullException("aws");
            }

            if (AWSServiceHandlerManifest == null)
            {
                _logger.DebugFormat("AWSServiceHandlerManifest doesn't exist.");
                return;
            }

            if (!AWSServiceHandlerManifest.Services.TryGetValue(serviceName, out AWSServiceHandler serviceHandler))
            {
                _logger.DebugFormat("Service name doesn't exist in AWSServiceHandlerManifest: serviceName = {0}.", serviceName);
                return;
            }

            if (!serviceHandler.Operations.TryGetValue(operation, out AWSOperationHandler operationHandler))
            {
                _logger.DebugFormat("Operation doesn't exist in AwsServiceInfo: serviceName = {0}, operation = {1}.", serviceName, operation);
                return;
            }

            if (operationHandler.ResponseParameters != null)
            {
                foreach (string parameter in operationHandler.ResponseParameters)
                {
                    if (TryReadPropertyValue(response, parameter, out object propertyValue))
                    {
                        aws[parameter.FromCamelCaseToSnakeCase()] = propertyValue;
                    }
                }
            }

            if (operationHandler.ResponseDescriptors != null)
            {
                foreach (KeyValuePair<string, AWSOperationResponseDescriptor> kv in operationHandler.ResponseDescriptors)
                {
                    var propertyName = kv.Key;
                    var descriptor = kv.Value;

                    if (descriptor.Map && descriptor.GetKeys)
                    {
                        AWSSdkTracingHandler.AddMapProperty(aws, response, propertyName, descriptor.RenameTo);
                    }
                    else if (descriptor.List && descriptor.GetCount)
                    {
                        AWSSdkTracingHandler.AddListProperty(aws, response, propertyName, descriptor.RenameTo);
                    }
                }
            }
        }
    }
}