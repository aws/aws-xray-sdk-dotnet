//-----------------------------------------------------------------------------
// <copyright file="RuleCache.cs" company="Amazon.com">
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Cache sampling rules and quota retrieved by <see cref="RulePoller"/>
    /// and <see cref="TargetPoller"/>. It will not return anything if the cache expires.
    /// </summary>
    public class RuleCache
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(RuleCache));
        private IDictionary<string, SamplingRule> _cache;
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        /// <summary>
        /// Stores timestamp for the last refreshed cache.
        /// </summary>
        public TimeStamp LastUpdated { get; set; }

        /// <summary>
        /// Cache expiry TTL.
        /// </summary>
        public const int TTL = 60 * 60; // cache expires 1 hour after the refresh (in sec)

        public RuleCache()
        {
            _cache = new Dictionary<string, SamplingRule>();
            LastUpdated = new TimeStamp();
        }

        /// <summary>
        /// Returns matched rule for the given <see cref="SamplingInput"/>.
        /// </summary>
        /// <param name="input">Instance of <see cref="SamplingInput"/>.</param>
        /// <param name="time">Current time.</param>
        /// <returns>Instance of <see cref="SamplingRule"/>.</returns>
        public SamplingRule GetMatchedRule(SamplingInput input, TimeStamp time)
        {
            if (IsExpired(time))
            {
                return null;
            }

            SamplingRule matchedRule = null;
            _cacheLock.EnterReadLock();
            try
            {
                foreach (var item in _cache)
                {
                    var rule = item.Value;
                    if (matchedRule != null)
                    {
                        break;
                    }

                    if (rule.Match(input) || rule.IsDefault())
                    {
                        matchedRule = rule;
                    }
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return matchedRule;
        }

        private bool IsExpired(TimeStamp current)
        {
            if (LastUpdated.Time == 0) // The cache is treated as expired if never loaded
            {
                return true;
            }
            return current.Time > LastUpdated.Time + TTL;
        }

        /// <summary>
        /// Returns list of rules present in the cache.
        /// </summary>
        /// <returns>List of <see cref="SamplingRule"/>.</returns>
        public IList<SamplingRule> GetRules()
        {
            _cacheLock.EnterReadLock();
            try
            {
                return _cache.Values.ToList();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Updates Targets in the cache
        /// </summary>
        /// <param name="targets"> Targets returned by GetSamplingTargets.</param>
        public void LoadTargets(IList<Target> targets)
        {
            _cacheLock.EnterReadLock();
            try
            {
                TimeStamp now = TimeStamp.CurrentTime();
                foreach (var t in targets)
                {
                    if (_cache.TryGetValue(t.RuleName, out SamplingRule rule))
                    {
                        rule.Reservoir.LoadQuota(t, now);
                        rule.SetRate(t.FixedRate);
                    }
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds new rules to cache, clearing old rules not present in the newRules. 
        /// </summary>
        /// <param name="newRules"> Rules returned by GetSampling API.</param>
        public void LoadRules(List<SamplingRule> newRules)
        {
            IDictionary<string, SamplingRule> oldRules;
            IDictionary<string, SamplingRule> tempCache = new Dictionary<string, SamplingRule>();

            newRules.Sort((x, y) => x.CompareTo(y)); // Sort by priority and rule name

            _cacheLock.EnterReadLock();
            try
            {
                oldRules = new Dictionary<string, SamplingRule>(_cache);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            foreach (var newRule in newRules)
            {
                var key = newRule.RuleName;
                tempCache[key] = newRule;
                if (oldRules.TryGetValue(key, out SamplingRule oldRule))
                {
                    newRule.Merge(oldRule);
                }
            }
            _cacheLock.EnterWriteLock();
            try
            {
                _cache = tempCache; // update cache
            }

            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
    }
}
