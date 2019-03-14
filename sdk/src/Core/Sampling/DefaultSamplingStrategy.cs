//-----------------------------------------------------------------------------
// <copyright file="DefaultSamplingStrategy.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Sampling.Local;
using System;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Making sampling decisions based on sampling rules defined
    /// by X-Ray control plane APIs.It will fall back to <see cref="LocalizedSamplingStrategy"/> if
    /// sampling rules are not available.
    /// </summary>
    public class DefaultSamplingStrategy : ISamplingStrategy
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(DefaultSamplingStrategy));
        private ISamplingStrategy _localFallbackRules;
        private RuleCache _ruleCache;
        private RulePoller _rulePoller;
        private TargetPoller _targetPoller;
        private IConnector _connector;
        private bool _isPollerStarted = false;
        private readonly Object _lock = new Object();
        /// <summary>
        /// Instance of <see cref="DaemonConfig"/>.
        /// </summary>
        public DaemonConfig DaemonCfg { get; private set; }

        /// <summary>
        /// Instance of <see cref="AmazonXRayClient"/>.
        /// </summary>
        public AmazonXRayClient XRayClient = null;

        /// <summary>
        /// Instance of <see cref="DefaultSamplingStrategy"/>.
        /// </summary>
        public DefaultSamplingStrategy()
        {
            _localFallbackRules = new LocalizedSamplingStrategy(); 
            InititalizeStrategy();
        }

        /// <summary>
        /// Instance of <see cref="DefaultSamplingStrategy"/>.
        /// </summary>
        /// <param name="samplingRuleManifest">Path to local sampling maifest file.</param>
        public DefaultSamplingStrategy(string samplingRuleManifest)
        {
            _localFallbackRules = new LocalizedSamplingStrategy(samplingRuleManifest); 
            InititalizeStrategy();
        }
        private void InititalizeStrategy()
        {
            _ruleCache = new RuleCache();
            _rulePoller = new RulePoller(_ruleCache);
            _targetPoller = new TargetPoller(_ruleCache, _rulePoller);
        }

        /// <summary>
        /// Start rule poller and target poller.
        /// </summary>
        private void Start()
        {
            lock (_lock)
            {
                if (!_isPollerStarted)
                {
                    _connector = new ServiceConnector(DaemonCfg, XRayClient);
                    _rulePoller.Poll(_connector);
                    _targetPoller.Poll(_connector);
                    _isPollerStarted = true;
                }
            }
        }

        /// <summary>
        /// Return the matched sampling rule name if the sampler finds one
        /// and decide to sample. If no sampling rule matched, it falls back
        /// to <see cref="LocalizedSamplingStrategy"/> "ShouldTrace" implementation.
        /// All optional arguments are extracted from incoming requests by
        /// X-Ray middleware to perform path based sampling.
        /// </summary>
        /// <param name="input">Instance of <see cref="SamplingInput"/>.</param>
        /// <returns>Instance of <see cref="SamplingResponse"/>.</returns>
        public SamplingResponse ShouldTrace(SamplingInput input)
        {
            if (!_isPollerStarted) // Start pollers lazily
            {
                Start();
            }

            if (string.IsNullOrEmpty(input.ServiceType))
            {
                input.ServiceType = AWSXRayRecorder.Instance.Origin;
            }

            TimeStamp time = TimeStamp.CurrentTime();
            SamplingRule sampleRule = _ruleCache.GetMatchedRule(input,time);
            if (sampleRule != null)
            {
                _logger.DebugFormat("Rule {0} is selected to make a sampling decision.", sampleRule.RuleName);
                return ProcessMatchedRule(sampleRule, time);
            }
            else
            {
                _logger.InfoFormat("No effective centralized sampling rule match. Fallback to local rules.");
                return _localFallbackRules.ShouldTrace(input);
            }
        }

        private SamplingResponse ProcessMatchedRule(SamplingRule sampleRule, TimeStamp time)
        {
            bool shouldSample = true;
            Reservoir reservoir = sampleRule.Reservoir;
            SamplingResponse sampleResult = null;

            sampleRule.IncrementRequestCount(); // increment request counter for matched rule
            ReservoirDecision reservoirDecision = reservoir.BorrowOrTake(time, sampleRule.CanBorrow); // check if we can borrow or take from reservoir

            if (reservoirDecision == ReservoirDecision.Borrow)
            {
                sampleRule.IncrementBorrowCount();
            }
            else if (reservoirDecision == ReservoirDecision.Take)
            {
                sampleRule.IncrementSampledCount();
            }
            else if (ThreadSafeRandom.NextDouble() <= sampleRule.GetRate()) // compute based on fixed rate
            {
                sampleRule.IncrementSampledCount();
            }
            else
            {
                shouldSample = false;
            }

            if (shouldSample)
            {
                sampleResult = new SamplingResponse(sampleRule.RuleName, SampleDecision.Sampled);
            }
            else
            {
                sampleResult = new SamplingResponse(SampleDecision.NotSampled);
            }

            return sampleResult;
        }

        /// <summary>
        /// Configures X-Ray client with given <see cref="DaemonConfig"/> instance.
        /// </summary>
        /// <param name="daemonConfig">An instance of <see cref="DaemonConfig"/>.</param>
        public void LoadDaemonConfig(DaemonConfig daemonConfig)
        {
            DaemonCfg = daemonConfig;
        }
    }
}
