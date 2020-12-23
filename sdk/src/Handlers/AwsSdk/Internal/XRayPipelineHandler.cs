//-----------------------------------------------------------------------------
// <copyright file="XRayPipelineHandler.cs" company="Amazon.com">
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
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Reflection;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Handlers.AwsSdk.Entities;
using ThirdParty.LitJson;
using Amazon.Runtime.Internal;
using System.Threading;
using Amazon.Runtime.Internal.Transform;
using Amazon.XRay.Recorder.Core.Exceptions;

namespace Amazon.XRay.Recorder.Handlers.AwsSdk.Internal
{
    /// <summary>
    /// The handler to register <see cref="Runtime.AmazonServiceClient"/> which can intercept downstream requests and responses.
    /// Note: This class should not be instantiated or used in anyway. It is used internally within SDK.
    /// </summary>
    public class XRayPipelineHandler : PipelineHandler
    {
        private const string DefaultAwsWhitelistManifestResourceName = "Amazon.XRay.Recorder.Handlers.AwsSdk.DefaultAWSWhitelist.json";
        private static readonly Logger _logger = Runtime.Internal.Util.Logger.GetLogger(typeof(AWSXRayRecorder));
        private AWSXRayRecorder _recorder;
        private const String S3RequestIdHeaderKey = "x-amz-request-id";
        private const String S3ExtendedRequestIdHeaderKey = "x-amz-id-2";
        private const String ExtendedRquestIdSegmentKey = "id_2";

        /// <summary>
        /// Gets AWS service manifest of operation parameter whitelist.
        /// </summary>
        public AWSServiceHandlerManifest AWSServiceHandlerManifest { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XRayPipelineHandler" /> class.
        /// </summary>
        public XRayPipelineHandler()
        {
            _recorder = AWSXRayRecorder.Instance;
            AWSServiceHandlerManifest  = GetDefaultAWSWhitelist();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XRayPipelineHandler" /> class.
        /// </summary>
        /// <param name="path">Path to the file which contains the operation parameter whitelist configuration.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when recorder is null.</exception>
        public XRayPipelineHandler(string path)
        {
            _recorder = AWSXRayRecorder.Instance;

            if (_recorder == null)
            {
                throw new ArgumentNullException("recorder");
            }

            AWSServiceHandlerManifest = GetAWSServiceManifest(path);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="XRayPipelineHandler" /> class.
        /// </summary>
        /// <param name="stream"> stream for manifest which contains the operation parameter whitelist configuration.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when recorder is null.</exception>
        public XRayPipelineHandler(Stream stream)
        {
            _recorder = AWSXRayRecorder.Instance;

            if (_recorder == null)
            {
                throw new ArgumentNullException("recorder");
            }

            AWSServiceHandlerManifest = GetAWSServiceManifest(stream);
        }

        /// <summary>
        /// Creates instance of <see cref="XRayPipelineHandler"/> with provided AWS service manifest instance.
        /// </summary>
        /// <param name="awsServiceManifest">Instance of <see cref="AWSServiceHandlerManifest"/></param>
        public XRayPipelineHandler(AWSServiceHandlerManifest awsServiceManifest)
        {
            _recorder = AWSXRayRecorder.Instance;

            if (_recorder == null)
            {
                throw new ArgumentNullException("recorder");
            }

            AWSServiceHandlerManifest = awsServiceManifest;
        }

        /// <summary>
        /// Extracts <see cref="AWSServiceHandlerManifest"/> instance from provided path of AWS Service manifest file.
        /// </summary>
        /// <param name="path">Absolute path to AWS Service Manifest file</param>
        /// <returns>Instance of <see cref="AWSServiceHandlerManifest"/></returns>
        public static AWSServiceHandlerManifest GetAWSServiceManifest(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.DebugFormat("The path is null or empty, initializing with default AWS whitelist.");
                return GetDefaultAWSWhitelist();
            }
            else
            {
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    return GetAWSServiceHandlerManifest(stream);
                }
            }
        }

        /// <summary>
        /// Extracts <see cref="AWSServiceHandlerManifest"/> instance from provided aws service manifest stream.
        /// </summary>
        /// <param name="stream">Absolute path to AWS Service Manifest file</param>
        /// <returns>Instance of <see cref="AWSServiceHandlerManifest"/></returns>
        public static AWSServiceHandlerManifest GetAWSServiceManifest(Stream stream)
        {
            if (stream == null)
            {
                _logger.DebugFormat("The provided stream is null, initializing with default AWS whitelist.");
                return GetDefaultAWSWhitelist();
            }
            else
            {
                return GetAWSServiceHandlerManifest(stream);
            }
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
        /// <returns>String after removing Amazon prefix.</returns>
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

        private static void AddMapKeyProperty(IDictionary<string, object> aws, object obj, string propertyName, string renameTo = null)
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

        private static void AddListLengthProperty(IDictionary<string, object> aws, object obj, string propertyName, string renameTo = null)
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

        private static AWSServiceHandlerManifest GetDefaultAWSWhitelist()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultAwsWhitelistManifestResourceName))
            {
                return GetAWSServiceHandlerManifest(stream);
            }
        }

        private static AWSServiceHandlerManifest GetAWSServiceHandlerManifest(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                try
                {
                    return JsonMapper.ToObject<AWSServiceHandlerManifest>(reader);
                }
                catch (JsonException e)
                {
                    _logger.Error(e, "Failed to load AWSServiceHandlerManifest.");
                }
                return null;
            }
        }

        /// <summary>
        /// Processes Begin request by starting subsegment.
        /// </summary>
        private void ProcessBeginRequest(IExecutionContext executionContext)
        {
            var request = executionContext.RequestContext.Request;
            Entity entity = null;
            try
            {
                entity = _recorder.GetEntity();
            }
            catch (EntityNotAvailableException e)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder, e, "Cannot get entity while processing AWS SDK request");
            }

            var serviceName = RemoveAmazonPrefixFromServiceName(request.ServiceName);
            _recorder.BeginSubsegment(AWSXRaySDKUtils.FormatServiceName(serviceName));
            _recorder.SetNamespace("aws");

            entity = entity == null ? null : _recorder.GetEntity();

            if (TraceHeader.TryParse(entity, out TraceHeader traceHeader))
            {
                request.Headers[TraceHeader.HeaderKey] = traceHeader.ToString();
            }
            else
            {
                _logger.DebugFormat("Failed to inject trace header to AWS SDK request as the segment can't be converted to TraceHeader.");
            }
        }

        /// <summary>
        /// Processes End request by ending subsegment.
        /// </summary>
        private void ProcessEndRequest(IExecutionContext executionContext)
        {
            Entity subsegment;
            try
            {
                subsegment = _recorder.GetEntity();
            }
            catch(EntityNotAvailableException e)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder,e,"Cannot get entity from the trace context while processing response of AWS SDK request.");
                return;
            }

            var responseContext = executionContext.ResponseContext;
            var requestContext = executionContext.RequestContext;

            if (responseContext == null)
            {
                _logger.DebugFormat("Failed to handle AfterResponseEvent, because response is null.");
                return;
            }

            var client = executionContext.RequestContext.ClientConfig;
            if (client == null)
            {
                _logger.DebugFormat("Failed to handle AfterResponseEvent, because client from the Response Context is null");
                return;
            }

            var serviceName = RemoveAmazonPrefixFromServiceName(requestContext.Request.ServiceName);
            var operation = RemoveSuffix(requestContext.OriginalRequest.GetType().Name, "Request");

            subsegment.Aws["region"] = client.RegionEndpoint?.SystemName;
            subsegment.Aws["operation"] = operation;
            if (responseContext.Response == null)
            {
                if (requestContext.Request.Headers.TryGetValue("x-amzn-RequestId", out string requestId))
                {
                    subsegment.Aws["request_id"] = requestId;
                }
                // s3 doesn't follow request header id convention
                else
                {
                    if (requestContext.Request.Headers.TryGetValue(S3RequestIdHeaderKey, out requestId))
                    {
                        subsegment.Aws["request_id"] = requestId;
                    }

                    if (requestContext.Request.Headers.TryGetValue(S3ExtendedRequestIdHeaderKey, out requestId))
                    {
                        subsegment.Aws[ExtendedRquestIdSegmentKey] = requestId;
                    }
                }
            }
            else
            {
                subsegment.Aws["request_id"] = responseContext.Response.ResponseMetadata.RequestId;

                // try getting x-amz-id-2 if dealing with s3 request
                if (responseContext.Response.ResponseMetadata.Metadata.TryGetValue(S3ExtendedRequestIdHeaderKey, out string extendedRequestId))
                {
                    subsegment.Aws[ExtendedRquestIdSegmentKey] = extendedRequestId;
                }

                AddResponseSpecificInformation(serviceName, operation, responseContext.Response, subsegment.Aws);
            }

            if (responseContext.HttpResponse != null)
            {
                AddHttpInformation(responseContext.HttpResponse);
            }

            AddRequestSpecificInformation(serviceName, operation, requestContext.OriginalRequest, subsegment.Aws);
            _recorder.EndSubsegment();
        }

        private void AddHttpInformation(IWebResponseData httpResponse)
        {
            var responseAttributes = new Dictionary<string, object>();
            int statusCode = (int)httpResponse.StatusCode;
            if (statusCode >= 400 && statusCode <= 499)
            {
                _recorder.MarkError();

                if (statusCode == 429)
                {
                    _recorder.MarkThrottle();
                }
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                _recorder.MarkFault();
            }

            responseAttributes["status"] = statusCode;
            responseAttributes["content_length"] = httpResponse.ContentLength;
            _recorder.AddHttpInformation("response", responseAttributes);
        }

        private void ProcessException(AmazonServiceException ex, Entity subsegment)
        {
            int statusCode = (int)ex.StatusCode;
            var responseAttributes = new Dictionary<string, object>();

            if (statusCode >= 400 && statusCode <= 499)
            {
                _recorder.MarkError();
                if (statusCode == 429)
                {
                    _recorder.MarkThrottle();
                }
            }
            else if (statusCode >= 500 && statusCode <= 599)
            {
                _recorder.MarkFault();
            }

            responseAttributes["status"] = statusCode;
            _recorder.AddHttpInformation("response", responseAttributes);

            subsegment.Aws["request_id"] = ex.RequestId;

            // Try to obtain x-amz-id-2 from s3 exception
            // https://github.com/aws/aws-sdk-net/blob/master/sdk/src/Services/S3/Custom/AmazonS3Exception.cs#L125
            var property = ex.GetType().GetProperty("AmazonId2");
            if (property != null)
            {
                subsegment.Aws[ExtendedRquestIdSegmentKey] = (string)property.GetValue(ex, null);
            }
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
                        AddMapKeyProperty(aws, request, propertyName, descriptor.RenameTo);
                    }
                    else if (descriptor.List && descriptor.GetCount)
                    {
                        AddListLengthProperty(aws, request, propertyName, descriptor.RenameTo);
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
                        XRayPipelineHandler.AddMapKeyProperty(aws, response, propertyName, descriptor.RenameTo);
                    }
                    else if (descriptor.List && descriptor.GetCount)
                    {
                        XRayPipelineHandler.AddListLengthProperty(aws, response, propertyName, descriptor.RenameTo);
                    }
                }
            }
        }

        /// <summary>
        /// Process Synchronous <see cref="AmazonServiceClient"/> operations. A subsegment is started at the beginning of 
        /// the request and ended at the end of the request.
        /// </summary>
        public override void InvokeSync(IExecutionContext executionContext)
        {
            if (IsTracingDisabled() || ExcludeServiceOperation(executionContext))
            {
                base.InvokeSync(executionContext);
            }
            else
            {
                ProcessBeginRequest(executionContext);

                try
                {
                    base.InvokeSync(executionContext);
                }

                catch (Exception e)
                {
                    PopulateException(e);
                    throw;
                }

                finally
                {
                    ProcessEndRequest(executionContext);
                }
            }
        }

        private void PopulateException(Exception e)
        {
            Entity subsegment;
            try
            {
                subsegment = _recorder.GetEntity();
            }
            catch (EntityNotAvailableException ex)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder, ex, "Cannot get entity from trace context while processing exception for AWS SDK request.");
                return;
            }

            subsegment.AddException(e); // record exception 

            if (e is AmazonServiceException amazonServiceException)
            {
                ProcessException(amazonServiceException, subsegment);
            }
            return;
        }

        private bool ExcludeServiceOperation(IExecutionContext executionContext)
        {
            var requestContext = executionContext.RequestContext;
            var serviceName = RemoveAmazonPrefixFromServiceName(requestContext.Request.ServiceName);
            var operation = RemoveSuffix(requestContext.OriginalRequest.GetType().Name, "Request");

            return AWSXRaySDKUtils.IsBlacklistedOperation(serviceName,operation);
        }

        private bool IsTracingDisabled()
        {
            if (AWSXRayRecorder.Instance.IsTracingDisabled())
            {
                _logger.DebugFormat("X-Ray tracing is disabled, do not handle AWSSDK request / response.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Process Asynchronous <see cref="AmazonServiceClient"/> operations. A subsegment is started at the beginning of 
        /// the request and ended at the end of the request.
        /// </summary>
        public override async Task<T> InvokeAsync<T>(IExecutionContext executionContext)
        {
            T ret = null;

            if (IsTracingDisabled() || ExcludeServiceOperation(executionContext))
            {
                ret = await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
            }
            else
            {
                ProcessBeginRequest(executionContext);

                try
                {
                    ret = await base.InvokeAsync<T>(executionContext).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    PopulateException(e);
                    throw;
                }

                finally
                {
                    ProcessEndRequest(executionContext);
                }

            }

            return ret;
        }
    }

    /// <summary>
    /// Pipeline Customizer for registering <see cref="AmazonServiceClient"/> instances with AWS X-Ray.
    /// Note: This class should not be instantiated or used in anyway. It is used internally within SDK.
    /// </summary>
    public class XRayPipelineCustomizer : IRuntimePipelineCustomizer
    {
        public string UniqueName { get { return "X-Ray Registration Customization"; } }
        private Boolean registerAll;
        private List<Type> types = new List<Type>();
        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
   
        public bool RegisterAll { get => registerAll; set => registerAll = value; }
        public string Path { get; set; } // TODO :: This is not used anymore, remove in next breaking change
        public XRayPipelineHandler XRayPipelineHandler { get; set; } = null; // TODO :: This is not used anymore, remove in next breaking change
        public AWSServiceHandlerManifest AWSServiceHandlerManifest { get; set; } = null;

        public void Customize(Type serviceClientType, RuntimePipeline pipeline)
        {
            if (serviceClientType.BaseType != typeof(AmazonServiceClient))
                return;

            bool addCustomization = this.RegisterAll;

            if (!addCustomization)
            {
                addCustomization = ProcessType(serviceClientType, addCustomization);
            }

            if (addCustomization && AWSServiceHandlerManifest == null)
            {
                pipeline.AddHandlerAfter<EndpointResolver>(new XRayPipelineHandler());
            }
            else if (addCustomization && AWSServiceHandlerManifest != null)
            {
                pipeline.AddHandlerAfter<EndpointResolver>(new XRayPipelineHandler(AWSServiceHandlerManifest)); // Custom AWS Manifest file path/stream provided
            }
        }

        private bool ProcessType(Type serviceClientType, bool addCustomization)
        {
            rwLock.EnterReadLock();

            try
            {
                foreach (var registeredType in types)
                {
                    if (registeredType.IsAssignableFrom(serviceClientType))
                    {
                        addCustomization = true;
                        break;
                    }
                }
            }
            finally
            {
                rwLock.ExitReadLock();
            }

            return addCustomization;
        }

        /// <summary>
        /// Adds type to the list of <see cref="Type" />.
        /// </summary>
        /// <param name="type"> Type of <see cref="Runtime.AmazonServiceClient"/> to be registered with X-Ray.</param>
        public void AddType(Type type)
        {
            rwLock.EnterWriteLock();

            try
            {
                types.Add(type);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }
}
