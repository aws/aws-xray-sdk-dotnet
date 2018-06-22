//-----------------------------------------------------------------------------
// <copyright file="TargetPoller.cs" company="Amazon.com">
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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// The poller to report the current statistics of all
    /// sampling rules and retrieve the new allocated
    /// sampling quota and TTL from X-Ray service.
    /// </summary>
    public class TargetPoller
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(TargetPoller));
        private RuleCache _ruleCache;
        private RulePoller _rulePoller;
        private IConnector _connector;
        private const int RefreshInterval = 10 * 1000; // 10 seconds
        private const double MaxJitter = 0.1 * 1000; // adding max jitter upto 0.1 seconds
        private readonly Random _random = new Random();
        private Timer _timer;

        public TargetPoller(RuleCache ruleCache, RulePoller rulePoller)
        {
            _ruleCache = ruleCache;
            _rulePoller = rulePoller;
        }

        internal void Poll(IConnector connector)
        {
            _connector = connector;
            _timer = InitializeTimer();
        }

        private Timer InitializeTimer()
        {
            return new Timer(Start, null, GetDelay(), 0); // First execution after GetDelay() time
        }

        internal async void Start(Object state)
        {
            try
            {
                await RefreshTargets();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Encountered exception while polling targets");
            }
            finally
            {
                _timer.Change(GetDelay(), 0);
            }
        }

        internal async Task RefreshTargets()
        {
            List<SamplingRule> rules = GetCandidates();
            if(rules == null || rules.Count == 0)
            {
                _logger.DebugFormat("There is no sampling rule statistics to report, skipping.");
                return;
            }
            _logger.DebugFormat("Reporting rule statistics to get new quota.");
            GetSamplingTargetsResponse response = await _connector.GetSamplingTargets(rules);
            _ruleCache.LoadTargets(response.Targets);

            if (response.RuleFreshness.IsGreaterThan(_ruleCache.LastUpdated))
            {
                _logger.InfoFormat("Performing out-of-band sampling rule polling to fetch updated rules.");
                _rulePoller.WakeUp();
            }
        }
        
        /// <summary>
        /// Don't report a rule statistics if any of the conditions is met:
        /// 1. The report time hasn't come (some rules might have larger report intervals).
        /// 2. The rule is never matched.
        /// </summary>
        /// <returns>List of <see cref="SamplingRule"/>.</returns>
        private List<SamplingRule> GetCandidates()
        {
            List<SamplingRule> candidates = new List<SamplingRule>();
            IList<SamplingRule> rules = _ruleCache.GetRules();
            TimeStamp now = TimeStamp.CurrentTime();
            foreach (var rule in rules)
            {
                if (rule.ShouldReport(now))
                {
                    candidates.Add(rule);
                }
            }

            return candidates;
        }

        private int GetDelay()
        {
            return RefreshInterval + GenerateRandomJitter();
        }

        private int GenerateRandomJitter()
        {
            return _random.Next(1, (int) MaxJitter);
        }
    }
}
