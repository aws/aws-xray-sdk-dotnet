//-----------------------------------------------------------------------------
// <copyright file="RuleCacheTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Sampling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class RuleCacheTests : TestBase
    {
        [TestMethod]
        public void TestLoadRules1() // Test basic LoadRules
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingRule rule = GetSamplingRule("a", 1, 0.5, 10, "test", "test", "GET", "/api/5");
            newRules.Add(rule);
            ruleCache.LoadRules(newRules);
            IList<SamplingRule> rulesInCache = ruleCache.GetRules();
            Assert.AreEqual(1,rulesInCache.Count);
        }

        [TestMethod]
        public void TestLoadRulesSort() // Test whether cache is sorted after LoadRules by priority and then by ruleName
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingRule rule1 = GetSamplingRule("a", 2);
            SamplingRule rule2 = GetSamplingRule("b", 1);
            SamplingRule rule3 = GetSamplingRule("c", 3);
            SamplingRule rule4 = GetSamplingRule("d", 3);
            newRules.Add(rule1);
            newRules.Add(rule2);
            newRules.Add(rule3);
            newRules.Add(rule4);
            ruleCache.LoadRules(newRules);

            IList<SamplingRule> rulesInCache = ruleCache.GetRules();

            Assert.AreEqual(4, rulesInCache.Count);
            Assert.AreEqual(rule2.RuleName, rulesInCache[0].RuleName);
            Assert.AreEqual(rule1.RuleName, rulesInCache[1].RuleName);
            Assert.AreEqual(rule3.RuleName, rulesInCache[2].RuleName);
            Assert.AreEqual(rule4.RuleName, rulesInCache[3].RuleName);
        }

        [TestMethod]
        public void TestLoadTargets() // Test statefule attributes of already present rule in cache is carried to new rules.
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingRule rule1 = GetSamplingRule("a", 2);
            SamplingRule rule2 = GetSamplingRule("b", 1);
           
            newRules.Add(rule1);
            newRules.Add(rule2);

            ruleCache.LoadRules(newRules);

            IList<SamplingRule> rulesInCache = ruleCache.GetRules();

            Assert.AreEqual(2, rulesInCache.Count);
            Assert.AreEqual(rule2.RuleName, rulesInCache[0].RuleName);
            Assert.AreEqual(rule1.RuleName, rulesInCache[1].RuleName);

            // Load Targets
            DateTime dateTime = TimeStamp.CurrentDateTime();
            TimeStamp currentTime = new TimeStamp(dateTime);

            Target t1 = new Target("a", 0.9, 10, dateTime, 10);
            Target t2 = new Target("b", 0.5, 10, dateTime, 10);
            List<Target> newTargets = new List<Target>();

            newTargets.Add(t1);
            newTargets.Add(t2);

            ruleCache.LoadTargets(newTargets);

            rulesInCache = ruleCache.GetRules(); // check targets value is copied

            Reservoir actualReservoir2 = rulesInCache[0].Reservoir;
            Assert.AreEqual(currentTime.Time, actualReservoir2.TTL.Time);
            Assert.AreEqual(t2.Interval, actualReservoir2.Interval);
            Assert.AreEqual(t2.ReservoirQuota, actualReservoir2.Quota);

            Reservoir actualReservoir1 = rulesInCache[1].Reservoir;
            Assert.AreEqual(currentTime.Time, actualReservoir1.TTL.Time);
            Assert.AreEqual(t1.Interval, actualReservoir1.Interval);
            Assert.AreEqual(t1.ReservoirQuota, actualReservoir1.Quota);

        }

        [TestMethod]
        public void TestMerge() // Test statefule attributes of already present rule in cache is carried to new rules.
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingRule rule1 = GetSamplingRule("a", 2);
            Statistics expectedStatistic = new Statistics(2, 1, 1);
            rule1.Statistics = expectedStatistic;
            SamplingRule rule2 = GetSamplingRule("b", 1);
            newRules.Add(rule1);
            newRules.Add(rule2);

            ruleCache.LoadRules(newRules);

            IList<SamplingRule> rulesInCache = ruleCache.GetRules();

            Assert.AreEqual(2, rulesInCache.Count);
            Assert.AreEqual(rule2.RuleName, rulesInCache[0].RuleName);
            Assert.AreEqual(rule1.RuleName, rulesInCache[1].RuleName);

            // Load Targets
            DateTime dateTime = TimeStamp.CurrentDateTime();
            TimeStamp currentTime = new TimeStamp(dateTime);

            Target t1 = new Target("a", 0.9, 10, dateTime, 10);
            Target t2 = new Target("b", 0.5, 10, dateTime, 10);
            List<Target> newTargets = new List<Target>();

            newTargets.Add(t1);
            newTargets.Add(t2);

            ruleCache.LoadTargets(newTargets);
            rulesInCache = ruleCache.GetRules();
            rule1 = GetSamplingRule("a", 1);
            newRules = new List<SamplingRule>();
            newRules.Add(rule1);

            ruleCache.LoadRules(newRules); // In the next iteration, rule a with update prioirty and rule b is deleted.

            rulesInCache = ruleCache.GetRules();

            Assert.AreEqual(1, rulesInCache.Count); // only one rule should be present and statefule info should be carried from old rule
            Assert.AreEqual(1, rulesInCache[0].Priority); // implies new rule is added to cache

            Reservoir actualReservoir1 = rulesInCache[0].Reservoir;
            Assert.AreEqual(currentTime.Time, actualReservoir1.TTL.Time);
            Assert.AreEqual(t1.Interval, actualReservoir1.Interval);
            Assert.AreEqual(t1.ReservoirQuota, actualReservoir1.Quota);

            Statistics statistics = rulesInCache[0].Statistics;
            Assert.AreEqual(expectedStatistic.RequestCount, statistics.RequestCount);
            Assert.AreEqual(expectedStatistic.BorrowCount, statistics.BorrowCount);
            Assert.AreEqual(expectedStatistic.SampledCount, statistics.SampledCount);
        }

        [TestMethod]
        public void TestGetMatchedRule1() // Test basic matching rule
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "AWS::ECS::Container");
            SamplingRule expectedRule = GetSamplingRule("a", 1, 0.5, 10, samplingInput.Host, samplingInput.ServiceName, samplingInput.Method, samplingInput.Url, serviceType: samplingInput.ServiceType);
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput,current);
    
            Assert.IsTrue(actualRule.Equals(expectedRule));
        }

        [TestMethod]
        public void TestGetMatchedRule2() // ServiceType is different
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "AWS::ECS::Container");
            SamplingRule expectedRule = GetSamplingRule("a", 1, 0.5, 10, samplingInput.Host, samplingInput.ServiceName, samplingInput.Method, samplingInput.Url, serviceType:"XYZ");
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsNull(actualRule);
        }

        [TestMethod]
        public void TestGetMatchedRule3() // sampling input ServiceName and ServiceType is null - it should be ignored for rule matching
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "", "");
            SamplingRule expectedRule = GetSamplingRule("a", 1, 0.5, 10, samplingInput.Host, "test", samplingInput.Method, samplingInput.Url, serviceType: "XYZ");
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsTrue(actualRule.Equals(expectedRule));
        }

        [TestMethod]
        public void TestGetMatchedRule4() // sampling input has only ServiceName - the rule should be matched 
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk");
            SamplingRule expectedRule = GetSamplingRule("a", 1, 0.5, 10, serviceName: samplingInput.ServiceName);
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsTrue(actualRule.Equals(expectedRule));
        }

        [TestMethod]
        public void TestGetMatchedRuleNotMatching() // Rule not matched
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "*");
            SamplingRule rule = GetSamplingRule("a", 1, 0.5, 10, "j", samplingInput.ServiceName, samplingInput.Method, samplingInput.Url);
            newRules.Add(rule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsNull(actualRule);
        }

        [TestMethod]
        public void TestGetMatchedRuleNotMatching2() // SamplingInput with only ServiceName - Rule not matched
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk");
            SamplingRule rule = GetSamplingRule("a", 1, 0.5, 10, serviceName: "XYZ");
            newRules.Add(rule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsNull(actualRule);
        }

        [TestMethod]
        public void TestGetMatchedRuleWithDefaultRule() // Matching with default Rule;
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "*");
            SamplingRule rule = GetSamplingRule("a", 1, 0.5, 10, "j", "test", samplingInput.Method, samplingInput.Url);
            newRules.Add(rule);
            SamplingRule expectedRule = GetSamplingRule(SamplingRule.Default, 10000, 0.5, 1, "j", "*", "*","*"); // should match to default rule
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsTrue(actualRule.Equals(expectedRule));
        }

        [TestMethod]
        public void TestGetMatchedRuleWithDefaultRule2() // SamplingInput with only ServiceName - Matching with default Rule;
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk");
            SamplingRule rule = GetSamplingRule("a", 1, 0.5, 10, serviceName: "XYZ");
            newRules.Add(rule);
            SamplingRule expectedRule = GetSamplingRule(SamplingRule.Default, 10000, 0.5, 1, "j", "*", "*", "*"); // should match to default rule
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated = current;

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current);

            Assert.IsTrue(actualRule.Equals(expectedRule));
        }

        [TestMethod]
        public void TestGetMatchedRuleForExpiredCache1() // Cache not loaded, hould return null
        {
            RuleCache ruleCache = new RuleCache();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "*");
            TimeStamp current = TimeStamp.CurrentTime();

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current); // cache is expired since never loaded

            Assert.IsNull(actualRule);
        }

        [TestMethod]
        public void TestGetMatchedRuleForExpiredCache2() // Expired cache should not match rule.
        {
            RuleCache ruleCache = new RuleCache();
            List<SamplingRule> newRules = new List<SamplingRule>();
            SamplingInput samplingInput = new SamplingInput("elasticbeanstalk", "/api/1", "GET", "dynamo", "*");
            SamplingRule expectedRule = GetSamplingRule("a", 1, 0.5, 10, samplingInput.Host, samplingInput.ServiceName, samplingInput.Method, samplingInput.Url);
            newRules.Add(expectedRule);
            ruleCache.LoadRules(newRules);
            TimeStamp current = TimeStamp.CurrentTime();
            ruleCache.LastUpdated.Time = current.Time;
            current.Time += RuleCache.TTL + 1; 

            var actualRule = ruleCache.GetMatchedRule(samplingInput, current); // cache is expired, though matching rule is present

            Assert.IsNull(actualRule);
        }
        private SamplingRule GetSamplingRule(string ruleName, int priority, double fixedRate = 0, int reservoirSize = 0, string host = null, string serviceName = null, string httpMethod = null, string urlPath = null, string serviceType = "*", string resourceARN = "*", Dictionary<string,string> attributes = null)
        {
            return new SamplingRule(ruleName,priority,fixedRate,reservoirSize,host,serviceName,httpMethod,urlPath,serviceType,resourceARN,attributes);
        }
    }
}
