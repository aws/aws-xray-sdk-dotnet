//-----------------------------------------------------------------------------
// <copyright file="ServiceConnector.cs" company="Amazon.com">
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
using Amazon.XRay.Model;
using System.Threading.Tasks;
using System;
using Amazon.Runtime;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Connector class that translates Sampling poller functions to 
    /// actual X-Ray back-end APIs and communicates with X-Ray daemon as the
    /// signing proxy.
    /// </summary>
    class ServiceConnector : IConnector
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(ServiceConnector));
        private AmazonXRayClient _xrayClient;
        private readonly object _xrayClientLock = new object();
        private const int Version = 1;
        private readonly AmazonXRayConfig _config = new AmazonXRayConfig();
        private readonly AWSCredentials _credentials = new AnonymousAWSCredentials(); // sends unsigned requests to daemon endpoint
        private readonly DaemonConfig _daemonConfig;

        /// <summary>
        /// Client id for the instance. Its 24 digit hex number.
        /// </summary>
        public string ClientID;

        public ServiceConnector(DaemonConfig daemonConfig, AmazonXRayClient xrayClient) 
        {
            ClientID = ThreadSafeRandom.GenerateHexNumber(24);
            if (daemonConfig == null)
            {
                daemonConfig = DaemonConfig.GetEndPoint();
            }
            _daemonConfig = daemonConfig;
           
            if (xrayClient == null)
            {
                xrayClient = CreateXRayClient();
            }

            _xrayClient = xrayClient;
        }

        private AmazonXRayClient CreateXRayClient()
        {
            _config.ServiceURL = $"http://{_daemonConfig.TCPEndpoint.Address}:{_daemonConfig.TCPEndpoint.Port}";
            return new AmazonXRayClient(_credentials,_config);
        }

        private void RefreshEndPoint()
        {
            var serviceUrlCandidate = $"http://{_daemonConfig.TCPEndpoint.Address}:{_daemonConfig.TCPEndpoint.Port}";
         
            if (serviceUrlCandidate.Equals(_xrayClient.Config.ServiceURL)) return; // endpoint do not need refreshing

            _config.ServiceURL = serviceUrlCandidate;
            _xrayClient = new AmazonXRayClient(_credentials, _config);
            _logger.DebugFormat($"ServiceConnector Endpoint refreshed to: {_xrayClient.Config.ServiceURL}");
        }

        /// <summary>
        /// Use X-Ray client to get the sampling rules
        /// from X-Ray service.The call is proxied and signed by X-Ray Daemon.
        /// </summary>
        /// <returns></returns>
        public async Task<GetSamplingRulesResponse>  GetSamplingRules()
        {
            List<SamplingRule> newRules = new List<SamplingRule>();
            GetSamplingRulesRequest request = new GetSamplingRulesRequest();

            Task<Model.GetSamplingRulesResponse> responseTask;
            lock (_xrayClientLock)
            {
                RefreshEndPoint();
                responseTask = _xrayClient.GetSamplingRulesAsync(request);
            }
            var response = await responseTask;

            foreach(var record in response.SamplingRuleRecords)
            {
                var rule = record.SamplingRule;
                if (rule.Version == Version && SamplingRule.IsValid(rule)) // We currently only handle v1 sampling rules.
                {
                    var sampleRule = new SamplingRule(rule.RuleName, rule.Priority, rule.FixedRate, rule.ReservoirSize, rule.Host, rule.ServiceName, rule.HTTPMethod, rule.URLPath, rule.ServiceType, rule.ResourceARN, rule.Attributes);
                    newRules.Add(sampleRule);
                }
            }

            GetSamplingRulesResponse result = new GetSamplingRulesResponse(newRules);
            return result;
        }

        /// <summary>
        /// Report the current statistics of sampling rules and
        /// get back the new assigned quota/TTL/Interval from the X-Ray service.
        /// The call is proxied and signed via X-Ray Daemon.
        /// </summary>
        /// <param name="rules">List of <see cref="SamplingRule"/>.</param>
        /// <returns>Instance of <see cref="GetSamplingRulesResponse"/>.</returns>
        public async Task<GetSamplingTargetsResponse> GetSamplingTargets(List<SamplingRule> rules)
        {
            GetSamplingTargetsRequest request = new GetSamplingTargetsRequest();
            IList<Target> newTargets = new List<Target>();
            DateTime currentTime = TimeStamp.CurrentDateTime();
            List<SamplingStatisticsDocument> samplingStatisticsDocuments = GetSamplingStatisticsDocuments(rules, currentTime);
            request.SamplingStatisticsDocuments = samplingStatisticsDocuments;
            Task<Model.GetSamplingTargetsResponse> responseTask;
            lock (_xrayClientLock)
            {
                RefreshEndPoint();
                responseTask = _xrayClient.GetSamplingTargetsAsync(request);
            }
            var response = await responseTask;
            foreach (var record in response.SamplingTargetDocuments)
            {
                Target t = new Target(record.RuleName, record.FixedRate, record.ReservoirQuota, record.ReservoirQuotaTTL, record.Interval);
                newTargets.Add(t);
            }

            GetSamplingTargetsResponse result = new GetSamplingTargetsResponse(newTargets);
            result.RuleFreshness = new TimeStamp(response.LastRuleModification);
            return result;
        }

        private List<SamplingStatisticsDocument> GetSamplingStatisticsDocuments(List<SamplingRule> rules, DateTime currentTime)
        {
            List<SamplingStatisticsDocument> samplingStatisticsDocuments = new List<SamplingStatisticsDocument>();
            foreach (var rule in rules)
            {
                Statistics statistics = rule.SnapShotStatistics();
                SamplingStatisticsDocument doc = new SamplingStatisticsDocument();
                doc.ClientID = ClientID;
                doc.RuleName = rule.RuleName;
                doc.RequestCount = statistics.RequestCount;
                doc.SampledCount = statistics.SampledCount;
                doc.BorrowCount = statistics.BorrowCount;
                doc.Timestamp = currentTime;
                samplingStatisticsDocuments.Add(doc);
            }

            return samplingStatisticsDocuments;
        }
    }
}

