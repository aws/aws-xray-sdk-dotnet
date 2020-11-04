//-----------------------------------------------------------------------------
// <copyright file="SamplingRule.cs" company="Amazon.com">
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
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// It represents the Rules used for sampling.
    /// </summary> 
    public class SamplingRule : IComparable<SamplingRule>
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(SamplingRule));
        public string Host { get; set; }
        public string RuleName { get; set; }
        public int Priority { get; set; }
        public Reservior Reservior { get; set; }
        public double Rate { get; private set; }
        public int ReservoirSize { get; private set; }
        public string HTTPMethod { get; set; }
        public string ServiceName { get; set; }
        public string URLPath { get; private set; }
        public string ServiceType { get; private set; }
        public string ResourceARN { get; private set; }
        public Dictionary<string, string> Attributes { get; private set; }

        public Statistics Statistics;
        public bool CanBorrow { get; set; }

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public const String Default = "Default"; // Reserved keyword by X-Ray service

        public SamplingRule(string ruleName, int priority, double fixedRate, int reservoirSize, string host, string serviceName, string httpMethod, string urlPath, string serviceType, string resourceARN, Dictionary<string,string> attributes)
        {
            RuleName = ruleName;
            Priority = priority;
            Rate = fixedRate;
            ReservoirSize = reservoirSize;
            CanBorrow = reservoirSize > 0;
            ServiceName = serviceName;
            HTTPMethod = httpMethod;
            URLPath = urlPath;
            Host = host;
            ServiceType = serviceType;
            ResourceARN = resourceARN;
            Attributes = attributes;
            Reservior = new Reservior();
            Statistics = new Statistics();
        }

        internal void IncrementRequestCount()
        {
            Statistics.IncrementRequestCount();
        }

        internal void IncrementBorrowCount()
        {
            Statistics.IncrementBorrowCount();
        } 

        internal void IncrementSampledCount()
        {
            Statistics.IncrementSampledCount();
        }

        /// <summary>
        /// Validates sampling rule. ResourceARN with "*" value is valid. SDK doesn't support Atrributes parameter with any value.
        /// </summary>
        /// <param name ="rule">Instance of <see cref="Model.SamplingRuleModel"/></param>
        /// <returns> True, if the rule is valid else false.</returns>
        internal static bool IsValid(Model.SamplingRuleModel rule)
        {
            if (!string.Equals(rule.ResourceARN, "*"))
            {
                return false;
            }

            if (rule.Attributes != null && rule.Attributes.Count > 0)
            { 
                return false;
            }

            return true;
        }

        internal bool IsDefault()
        {
            return RuleName.Equals(Default);
        }

        /// <summary>
        /// Determines whether or not this sampling rule applies to the incoming
        /// request based on some of the request's parameters.
        /// </summary>
        /// <param name="input">Instance of <see cref="SamplingInput"/>.</param>
        /// <returns>True if the rule matches.</returns>
        internal bool Match(SamplingInput input)
        {
            try
            {
                return StringExtension.IsMatch(input.ServiceName, ServiceName) && StringExtension.IsMatch(input.Method, HTTPMethod) && StringExtension.IsMatch(input.Url, URLPath) && StringExtension.IsMatch(input.Host, Host) && StringExtension.IsMatch(input.ServiceType, ServiceType);
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Error(e, "Match rule timeout. Rule: serviceName = {0}, urlPath = {1}, httpMethod = {2}, host = {3}, serviceType = {4}. Input: serviceNameToMatch = {5}, urlPathToMatch = {6}, httpMethodToMatch = {7}, hostToMatch = {8}, serviceTypeToMatch = {9}.", ServiceName, URLPath,
                    HTTPMethod, Host, ServiceType, input.ServiceName, input.Url, input.Method, input.Host, input.ServiceType);

                return false;
            }
        }

        internal void SetRate(double fixedRate)
        {
            _lock.EnterWriteLock();
            try
            {
                Rate = fixedRate;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal double GetRate()
        {
            _lock.EnterReadLock();
            try
            {
                return Rate;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        internal bool ShouldReport(TimeStamp now)
        {
            if (EverMatched() && Reservior.ShouldReport(now))
            {
                return true;
            }
            return false;
        }

        internal bool EverMatched()
        {
            return Statistics.GetRequestCount() > 0;
        }

        // Migrate all stateful attributes from the old rule
        internal void Merge(SamplingRule oldRule)
        {
            Statistics.CopyFrom(oldRule.Statistics);
            Reservior.CopyFrom(oldRule.Reservior);
            oldRule = null;
        }

        /// <summary>
        /// Returns current state of <see cref="Sampling.Statistics"/>.
        /// </summary>
        /// <returns>Instance of <see cref="Sampling.Statistics"/>.</returns>
        internal Statistics SnapShotStatistics()
        {
            return Statistics.GetSnapShot();
        }

        public int CompareTo(SamplingRule y)
        {
            int result = this.Priority.CompareTo(y.Priority);
            if (result == 0)
            {
                result = this.RuleName.CompareTo(y.RuleName);
            }
            return result;
        }
    }
}