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
using System.Threading.Tasks;
using System;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Sampling.Model;
using System.Net;
using ThirdParty.LitJson;
using System.IO;
using System.Text;

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
        private XRayConfig _xrayConfig;
        private readonly object _xrayClientLock = new object();
        private const int Version = 1;
        private readonly DaemonConfig _daemonConfig;
        private readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Client id for the instance. Its 24 digit hex number.
        /// </summary>
        public string ClientID;

        public ServiceConnector(DaemonConfig daemonConfig, XRayConfig xrayConfig) 
        {
            ClientID = ThreadSafeRandom.GenerateHexNumber(24);
            if (daemonConfig == null)
            {
                daemonConfig = DaemonConfig.GetEndPoint();
            }
            _daemonConfig = daemonConfig;
           
            if (xrayConfig == null)
            {
                xrayConfig = CreateXRayConfig();
            }

            _xrayConfig = xrayConfig;
        }

        private XRayConfig CreateXRayConfig()
        {
            var config = new XRayConfig();
            config.ServiceURL = $"http://{_daemonConfig.TCPEndpoint.Address}:{_daemonConfig.TCPEndpoint.Port}";
            return config;
        }

        private void RefreshEndPoint()
        {
            var serviceUrlCandidate = $"http://{_daemonConfig.TCPEndpoint.Address}:{_daemonConfig.TCPEndpoint.Port}";

            if (serviceUrlCandidate.Equals(_xrayConfig.ServiceURL)) return; // endpoint do not need refreshing

            _xrayConfig.ServiceURL = serviceUrlCandidate;
            _logger.DebugFormat($"ServiceConnector Endpoint refreshed to: {_xrayConfig.ServiceURL}");
        }

        /// <summary>
        /// Get the sampling rules from X-Ray service.The call is proxied and signed by X-Ray Daemon.
        /// </summary>
        /// <returns></returns>
        public async Task<GetSamplingRulesResponse> GetSamplingRules()
        {
            Task<WebResponse> responseTask;
            lock (_xrayClientLock)
            {
                RefreshEndPoint();
                responseTask = GetSamplingInfoAsync(_xrayConfig.ServiceURL + "/GetSamplingRules");
            }
            var response = await responseTask;

            var responseContent = GetResponseContent(response);

            List<SamplingRule> samplingRules = UnmarshallSamplingRuleResponse(responseContent);

            GetSamplingRulesResponse result = new GetSamplingRulesResponse(samplingRules);
            return result;
        }

        private Task<WebResponse> GetSamplingInfoAsync(string url, string content = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";

            if (content != null)
            {
                using (Stream dataStream = request.GetRequestStream())
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(content);
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
            }

            Task<WebResponse> responseTask = request.GetResponseAsync();

            return responseTask;
        }

        private List<SamplingRule> UnmarshallSamplingRuleResponse(string responseContent)
        {
            List<SamplingRule> samplingRules = new List<SamplingRule>();

            var samplingRuleResponse = JsonMapper.ToObject<SamplingRuleResponseModel>(responseContent);

            foreach (var samplingRuleRecord in samplingRuleResponse.SamplingRuleRecords)
            {
                var samplingRuleModel = samplingRuleRecord.SamplingRule;
                if (samplingRuleModel.Version.GetValueOrDefault() == Version && SamplingRule.IsValid(samplingRuleModel))
                {
                    var samplingRule = new SamplingRule
                    (
                        samplingRuleModel.RuleName,
                        samplingRuleModel.Priority.GetValueOrDefault(),
                        samplingRuleModel.FixedRate.GetValueOrDefault(),
                        samplingRuleModel.ReservoirSize.GetValueOrDefault(),
                        samplingRuleModel.Host,
                        samplingRuleModel.ServiceName,
                        samplingRuleModel.HTTPMethod,
                        samplingRuleModel.URLPath,
                        samplingRuleModel.ServiceType,
                        samplingRuleModel.ResourceARN,
                        samplingRuleModel.Attributes
                    );
                    samplingRules.Add(samplingRule);
                }
            }

            return samplingRules;
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
            DateTime currentTime = TimeStamp.CurrentDateTime();
            List<SamplingStatisticsDocumentModel> samplingStatisticsDocumentModels = GetSamplingStatisticsDocuments(rules, currentTime);
            var samplingStatisticsModel = new SamplingStatisticsModel();
            samplingStatisticsModel.SamplingStatisticsDocuments = samplingStatisticsDocumentModels;

            string requestContent = JsonMapper.ToJson(samplingStatisticsModel); // Marshall SamplingStatisticsDocument to json

            Task<WebResponse> responseTask;
            lock (_xrayClientLock)
            {
                RefreshEndPoint();
                responseTask = GetSamplingInfoAsync(_xrayConfig.ServiceURL + "/SamplingTargets", requestContent);
            }
            var response = await responseTask;

            var samplingTargetResponse = UnmarshallSamplingTargetResponse(response);

            var targetList = ConvertTargetList(samplingTargetResponse.SamplingTargetDocuments);

            GetSamplingTargetsResponse result = new GetSamplingTargetsResponse(targetList);
            result.RuleFreshness = new TimeStamp(ConvertDoubleToDateTime(samplingTargetResponse.LastRuleModification));
            return result;
        }

        private List<Target> ConvertTargetList(List<SamplingTargetModel> targetModels)
        {
            List<Target> result = new List<Target>();
            foreach (var targetModel in targetModels)
            {
                Target t = new Target
                (
                    targetModel.RuleName,
                    targetModel.FixedRate.GetValueOrDefault(),
                    targetModel.ReservoirQuota.GetValueOrDefault(),
                    ConvertDoubleToDateTime(targetModel.ReservoirQuotaTTL),
                    targetModel.Interval.GetValueOrDefault()
                );
                result.Add(t);
            }
            return result;
        }

        private SamplingTargetResponseModel UnmarshallSamplingTargetResponse(WebResponse response)
        {
            var responseContent = GetResponseContent(response);

            var samplingTargetResponse = JsonMapper.ToObject<SamplingTargetResponseModel>(responseContent);

            return samplingTargetResponse;
        }

        private string GetResponseContent(WebResponse response)
        {
            string responseContent = "";
            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    responseContent = streamReader.ReadToEnd();
                }
            }

            response.Close();

            return responseContent;
        }

        private List<SamplingStatisticsDocumentModel> GetSamplingStatisticsDocuments(List<SamplingRule> rules, DateTime currentTime)
        {
            List<SamplingStatisticsDocumentModel> samplingStatisticsDocumentModels = new List<SamplingStatisticsDocumentModel>();
            foreach (var rule in rules)
            {
                Statistics statistics = rule.SnapShotStatistics();
                SamplingStatisticsDocumentModel item = new SamplingStatisticsDocumentModel();
                item.ClientID = ClientID;
                item.RuleName = rule.RuleName;
                item.RequestCount = statistics.RequestCount;
                item.SampledCount = statistics.SampledCount;
                item.BorrowCount = statistics.BorrowCount;
                item.Timestamp = ConvertDateTimeToDouble(currentTime);
                samplingStatisticsDocumentModels.Add(item);
            }

            return samplingStatisticsDocumentModels;
        }

        private double ConvertDateTimeToDouble(DateTime currentTime)
        {
            var current = new TimeSpan(currentTime.ToUniversalTime().Ticks - EpochStart.Ticks);
            return Math.Round(current.TotalMilliseconds, 0) / 1000.0;
        }

        private DateTime ConvertDoubleToDateTime(double? seconds)
        {
            return seconds == null ? default(DateTime) : EpochStart.AddSeconds(seconds.GetValueOrDefault());
        }
    }
}
