//-----------------------------------------------------------------------------
// <copyright file="RulePoller.cs" company="Amazon.com">
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
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Gets sampling rules from X-Ray service. This is a asynchronous operation.
    /// </summary>
    public class RulePoller
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(RulePoller));
        private RuleCache _ruleCache;
        private IConnector _connector;
        private const int RefreshInterval = 5 * 60 * 1000; // 5 minutes
        private const int MaxJitter = 5 * 1000; // adding max jitter upto 5 seconds
        private readonly Random _random;
        private Timer _timer;

        internal TimeStamp TimeElasped { get; set; }
        internal TimeStamp TimeToWait { get; set; }

        public RulePoller(RuleCache ruleCache)
        {
            _ruleCache = ruleCache;
            _random = new Random();
        }

        internal void Poll(IConnector connector)
        {
            _connector = connector;
            _timer = InitializeTimer();
        }

        private Timer InitializeTimer()
        {
            return new Timer(Start, null, 0, 0);
        }

        internal async void Start(Object state)
        {
            try
            {
                await RefreshCache();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Encountered an exception while polling sampling rules.");
            }
            finally
            {
                _timer.Change(GetDelay(), 0); // Next invocation with jitter
            }
        }

        /// <summary>
        /// Gets sampling rules from the X-Ray service ans populates <see cref="RuleCache"/>.
        /// </summary>
        /// <returns>Task instance.</returns>
        private async Task RefreshCache()
        {
            TimeStamp time = TimeStamp.CurrentTime();
            GetSamplingRulesResponse response = await _connector.GetSamplingRules();
            if (response.IsRulePresent())
            {
                _ruleCache.LoadRules(response.Rules);
                _ruleCache.LastUpdated = time;
                _logger.InfoFormat("Successfully refreshed sampling rule cache.");
            }
        }
        /// <summary>
        /// Force the rule poller to pull the sampling rules from the service
        /// regardless of the polling interval.
        /// This method is intended to be used by <see cref="TargetPoller"/> only.
        /// </summary>
        internal void WakeUp()
        {
            _timer.Dispose();
            _timer = InitializeTimer(); // Perform out of band polling
        }

        private int GetDelay()
        {
            return RefreshInterval + GenerateRandomJitter();
        }

        /// <summary>
        /// A random jitter of up to 5 seconds is injected after each run
        /// to ensure the calls eventually get evenly distributed over
        /// the 5 minute window.
        /// </summary>
        /// <returns></returns>
        private int GenerateRandomJitter()
        {
            return _random.Next(1, MaxJitter);
        }
    }
}
