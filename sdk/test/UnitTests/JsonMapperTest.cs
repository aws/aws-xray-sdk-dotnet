//-----------------------------------------------------------------------------
// <copyright file="JsonMapperTest.cs" company="Amazon.com">
//      Copyright 2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

using Amazon.XRay.Recorder.Core.Sampling.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class JsonMapperTest
    {
        [TestMethod]
        public void TestMarshallSamplingStatisticsDocuments()
        {
            var samplingStatisticsModel = new SamplingStatisticsModel();
            var samplingStatisticsDocumentModel = new SamplingStatisticsDocumentModel
            {
                ClientID = "07492221d7fd13a86e750de2",
                RuleName = "Test",
                RequestCount = 108,
                SampledCount = 4,
                BorrowCount = 6,
                Timestamp = 1604297926.362
            };

            samplingStatisticsModel.SamplingStatisticsDocuments.Add(samplingStatisticsDocumentModel);

            var expected = "{\"SamplingStatisticsDocuments\":[{\"ClientID\":\"07492221d7fd13a86e750de2\",\"RuleName\":\"Test\",\"RequestCount\":108,\"SampledCount\":4,\"BorrowCount\":6,\"Timestamp\":1604297926.362}]}";
            var actual = JsonMapper.ToJson(samplingStatisticsModel);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallSamplingStatisticsDocumentsWithEmptyValues()
        {
            var samplingStatisticsModel = new SamplingStatisticsModel();
            var samplingStatisticsDocumentModel = new SamplingStatisticsDocumentModel();

            samplingStatisticsModel.SamplingStatisticsDocuments.Add(samplingStatisticsDocumentModel);

            var expected = "{\"SamplingStatisticsDocuments\":[{\"ClientID\":null,\"RuleName\":null,\"RequestCount\":null,\"SampledCount\":null,\"BorrowCount\":null,\"Timestamp\":null}]}";
            var actual = JsonMapper.ToJson(samplingStatisticsModel);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallSamplingStatisticsDocumentsWithEmptyItems()
        {
            var samplingStatisticsModel = new SamplingStatisticsModel();

            var expected = "{\"SamplingStatisticsDocuments\":[]}";
            var actual = JsonMapper.ToJson(samplingStatisticsModel);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestUnmarshallSamplingRuleResponse()
        {
            var samplingRuleResponseJson = "{\"NextToken\":null,\"SamplingRuleRecords\":[{\"CreatedAt\":0.0,\"ModifiedAt\":1.602621583E9,\"SamplingRule\":{\"Attributes\":{},\"FixedRate\":0.05,\"HTTPMethod\":\"*\",\"Host\":\"*\",\"Priority\":10000,\"ReservoirSize\":1,\"ResourceARN\":\"*\",\"RuleARN\":\"arn:aws:xray:us-east-1:1234567:sampling-rule/Default\",\"RuleName\":\"Default\",\"ServiceName\":\"*\",\"ServiceType\":\"*\",\"URLPath\":\"*\",\"Version\":1}}]}";

            var samplingRuleResponseModel = JsonMapper.ToObject<SamplingRuleResponseModel>(samplingRuleResponseJson);

            Assert.IsNull(samplingRuleResponseModel.NextToken);
            Assert.IsTrue(samplingRuleResponseModel.SamplingRuleRecords.Count > 0);

            foreach (var samplingRuleRecord in samplingRuleResponseModel.SamplingRuleRecords)
            {
                Assert.AreEqual(0.0, samplingRuleRecord.CreatedAt.GetValueOrDefault());
                Assert.AreEqual(1.602621583E9, samplingRuleRecord.ModifiedAt.GetValueOrDefault());
                Assert.IsNotNull(samplingRuleRecord.SamplingRule);
                Assert.IsTrue(samplingRuleRecord.SamplingRule.Attributes.Count == 0);
                Assert.AreEqual(0.05, samplingRuleRecord.SamplingRule.FixedRate.GetValueOrDefault());
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.HTTPMethod);
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.Host);
                Assert.AreEqual(10000, samplingRuleRecord.SamplingRule.Priority.GetValueOrDefault());
                Assert.AreEqual(1, samplingRuleRecord.SamplingRule.ReservoirSize.GetValueOrDefault());
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.ResourceARN);
                Assert.AreEqual("arn:aws:xray:us-east-1:1234567:sampling-rule/Default", samplingRuleRecord.SamplingRule.RuleARN);
                Assert.AreEqual("Default", samplingRuleRecord.SamplingRule.RuleName);
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.ServiceName);
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.ServiceType);
                Assert.AreEqual("*", samplingRuleRecord.SamplingRule.URLPath);
                Assert.AreEqual(1, samplingRuleRecord.SamplingRule.Version.GetValueOrDefault());
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingRuleResponseWithEmptyValues()
        {
            var samplingRuleResponseJson = "{\"NextToken\":null,\"SamplingRuleRecords\":[{\"CreatedAt\":null,\"ModifiedAt\":null,\"SamplingRule\":{\"Attributes\":{},\"FixedRate\":null,\"HTTPMethod\":null,\"Host\":null,\"Priority\":null,\"ReservoirSize\":null,\"ResourceARN\":null,\"RuleARN\":null,\"RuleName\":null,\"ServiceName\":null,\"ServiceType\":null,\"URLPath\":null,\"Version\":null}}]}";

            var samplingRuleResponseModel = JsonMapper.ToObject<SamplingRuleResponseModel>(samplingRuleResponseJson);

            Assert.IsNull(samplingRuleResponseModel.NextToken);
            Assert.IsTrue(samplingRuleResponseModel.SamplingRuleRecords.Count > 0);

            foreach (var samplingRuleRecord in samplingRuleResponseModel.SamplingRuleRecords)
            {
                Assert.AreEqual(0, samplingRuleRecord.CreatedAt.GetValueOrDefault());
                Assert.AreEqual(0, samplingRuleRecord.ModifiedAt.GetValueOrDefault());
                Assert.IsNotNull(samplingRuleRecord.SamplingRule);
                Assert.IsTrue(samplingRuleRecord.SamplingRule.Attributes.Count == 0);
                Assert.AreEqual(0, samplingRuleRecord.SamplingRule.FixedRate.GetValueOrDefault());
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.HTTPMethod);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.Host);
                Assert.AreEqual(0, samplingRuleRecord.SamplingRule.Priority.GetValueOrDefault());
                Assert.AreEqual(0, samplingRuleRecord.SamplingRule.ReservoirSize.GetValueOrDefault());
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.ResourceARN);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.RuleARN);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.RuleName);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.ServiceName);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.ServiceType);
                Assert.AreEqual(null, samplingRuleRecord.SamplingRule.URLPath);
                Assert.AreEqual(0, samplingRuleRecord.SamplingRule.Version.GetValueOrDefault());
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingRuleResponseWithInvalidFormat()
        {
            var samplingRuleResponseJson = "{\"a\":null,\"b\":[{\"c\":0.0,\"d\":1.602621583E9,\"e\":{\"f\":{},\"g\":0.05,\"h\":\"*\",\"i\":\"*\",\"j\":10000,\"k\":1,\"l\":\"*\",\"m\":\"arn:aws:xray:us-east-1:1234567:sampling-rule/Default\",\"n\":\"Default\",\"o\":\"*\",\"p\":\"*\",\"q\":\"*\",\"r\":1}}]}";

            var samplingRuleResponseModel = JsonMapper.ToObject<SamplingRuleResponseModel>(samplingRuleResponseJson);

            Assert.IsNull(samplingRuleResponseModel.NextToken);
            Assert.IsTrue(samplingRuleResponseModel.SamplingRuleRecords.Count == 0);
        }

        [TestMethod]
        public void TestUnmarshallSamplingRuleResponseWithNull()
        {
            string samplingRuleResponseJson = "";

            var samplingRuleResponseModel = JsonMapper.ToObject<SamplingRuleResponseModel>(samplingRuleResponseJson);

            Assert.IsNull(samplingRuleResponseModel);
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponse()
        {
            string samplingTargetResponseJson = "{\"LastRuleModification\":1.603923208E9,\"SamplingTargetDocuments\":[{\"FixedRate\":0.05,\"Interval\":2,\"ReservoirQuota\":1,\"ReservoirQuotaTTL\":1.5,\"RuleName\":\"Test\"}],\"UnprocessedStatistics\":[{\"ErrorCode\":\"400\",\"Message\":\"Unknown rule\",\"RuleName\":\"Fault\"}]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.AreEqual(1.603923208E9, samplingTargetResponseModel.LastRuleModification);
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count > 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count > 0);

            foreach (var target in samplingTargetResponseModel.SamplingTargetDocuments)
            {
                Assert.AreEqual(0.05, target.FixedRate.GetValueOrDefault());
                Assert.AreEqual(2, target.Interval.GetValueOrDefault());
                Assert.AreEqual(1, target.ReservoirQuota.GetValueOrDefault());
                Assert.AreEqual(1.5, target.ReservoirQuotaTTL.GetValueOrDefault());
                Assert.AreEqual("Test", target.RuleName);
            }

            foreach (var unprocessed in samplingTargetResponseModel.UnprocessedStatistics)
            {
                Assert.AreEqual("400", unprocessed.ErrorCode);
                Assert.AreEqual("Unknown rule", unprocessed.Message);
                Assert.AreEqual("Fault", unprocessed.RuleName);
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithExtraFields()
        {
            string samplingTargetResponseJson = "{\"LastRuleModification\":1.603923208E9,\"SamplingTargetDocuments\":[{\"Foo\":\"Bar\", \"FixedRate\":0.05,\"Interval\":2,\"ReservoirQuota\":1,\"ReservoirQuotaTTL\":1.5,\"RuleName\":\"Test\"}],\"UnprocessedStatistics\":[{\"ErrorCode\":\"400\",\"Message\":\"Unknown rule\",\"RuleName\":\"Fault\"}]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.AreEqual(1.603923208E9, samplingTargetResponseModel.LastRuleModification);
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count > 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count > 0);

            foreach (var target in samplingTargetResponseModel.SamplingTargetDocuments)
            {
                Assert.AreEqual(0.05, target.FixedRate.GetValueOrDefault());
                Assert.AreEqual(2, target.Interval.GetValueOrDefault());
                Assert.AreEqual(1, target.ReservoirQuota.GetValueOrDefault());
                Assert.AreEqual(1.5, target.ReservoirQuotaTTL.GetValueOrDefault());
                Assert.AreEqual("Test", target.RuleName);
            }

            foreach (var unprocessed in samplingTargetResponseModel.UnprocessedStatistics)
            {
                Assert.AreEqual("400", unprocessed.ErrorCode);
                Assert.AreEqual("Unknown rule", unprocessed.Message);
                Assert.AreEqual("Fault", unprocessed.RuleName);
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithoutSamplingTargetDocuments()
        {
            string samplingTargetResponseJson = "{\"LastRuleModification\":1.603923208E9,\"SamplingTargetDocuments\":[],\"UnprocessedStatistics\":[{\"ErrorCode\":\"400\",\"Message\":\"Unknown rule\",\"RuleName\":\"Fault\"}]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.AreEqual(1.603923208E9, samplingTargetResponseModel.LastRuleModification);
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count == 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count > 0);

            foreach (var unprocessed in samplingTargetResponseModel.UnprocessedStatistics)
            {
                Assert.AreEqual("400", unprocessed.ErrorCode);
                Assert.AreEqual("Unknown rule", unprocessed.Message);
                Assert.AreEqual("Fault", unprocessed.RuleName);
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithoutUnprocessedStatistics()
        {
            string samplingTargetResponseJson = "{\"LastRuleModification\":1.603923208E9,\"SamplingTargetDocuments\":[{\"FixedRate\":0.05,\"Interval\":2,\"ReservoirQuota\":1,\"ReservoirQuotaTTL\":1.5,\"RuleName\":\"Test\"}],\"UnprocessedStatistics\":[]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.AreEqual(1.603923208E9, samplingTargetResponseModel.LastRuleModification);
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count > 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count == 0);

            foreach (var target in samplingTargetResponseModel.SamplingTargetDocuments)
            {
                Assert.AreEqual(0.05, target.FixedRate.GetValueOrDefault());
                Assert.AreEqual(2, target.Interval.GetValueOrDefault());
                Assert.AreEqual(1, target.ReservoirQuota.GetValueOrDefault());
                Assert.AreEqual(1.5, target.ReservoirQuotaTTL.GetValueOrDefault());
                Assert.AreEqual("Test", target.RuleName);
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithEmptyValues()
        {
            string samplingTargetResponseJson = "{\"LastRuleModification\":null,\"SamplingTargetDocuments\":[{\"FixedRate\":null,\"Interval\":null,\"ReservoirQuota\":null,\"ReservoirQuotaTTL\":null,\"RuleName\":null}],\"UnprocessedStatistics\":[{\"ErrorCode\":null,\"Message\":null,\"RuleName\":null}]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.AreEqual(0, samplingTargetResponseModel.LastRuleModification.GetValueOrDefault());
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count > 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count > 0);

            foreach (var target in samplingTargetResponseModel.SamplingTargetDocuments)
            {
                Assert.AreEqual(0, target.FixedRate.GetValueOrDefault());
                Assert.AreEqual(0, target.Interval.GetValueOrDefault());
                Assert.AreEqual(0, target.ReservoirQuota.GetValueOrDefault());
                Assert.AreEqual(0, target.ReservoirQuotaTTL.GetValueOrDefault());
                Assert.IsNull(target.RuleName);
            }

            foreach (var unprocessed in samplingTargetResponseModel.UnprocessedStatistics)
            {
                Assert.IsNull(unprocessed.ErrorCode);
                Assert.IsNull(unprocessed.Message);
                Assert.IsNull(unprocessed.RuleName);
            }
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithInvalidFormat()
        {
            string samplingTargetResponseJson = "{\"a\":1.603923208E9,\"b\":[{\"c\":0.05,\"d\":2,\"e\":1,\"f\":1.5,\"g\":\"Test\"}],\"h\":[{\"i\":\"400\",\"j\":\"Unknown rule\",\"k\":\"Fault\"}]}";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.IsNull(samplingTargetResponseModel.LastRuleModification);
            Assert.IsTrue(samplingTargetResponseModel.SamplingTargetDocuments.Count == 0);
            Assert.IsTrue(samplingTargetResponseModel.UnprocessedStatistics.Count == 0);
        }

        [TestMethod]
        public void TestUnmarshallSamplingTargetResponseWithNull()
        {
            string samplingTargetResponseJson = "";

            var samplingTargetResponseModel = JsonMapper.ToObject<SamplingTargetResponseModel>(samplingTargetResponseJson);

            Assert.IsNull(samplingTargetResponseModel);
        }

        [TestMethod]
        public void SerializeObjectContainingEmptyGuid()
        {
            var obj = new AnythingWithGuid();
            var actual = JsonMapper.ToJson(obj);
            var expected = "{\"Id\":\"00000000-0000-0000-0000-000000000000\"}";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SerializeObjectContainingNonEmptyGuid()
        {
            var guid = Guid.Parse("b2aee583-770c-42c5-b5d8-d25e0df4d161");
            var obj = new AnythingWithGuid { Id = guid };
            var actual = JsonMapper.ToJson(obj);
            var expected = "{\"Id\":\"b2aee583-770c-42c5-b5d8-d25e0df4d161\"}";

            Assert.AreEqual(expected, actual);
        }

        public class AnythingWithGuid
        {
            public Guid Id { get; set; }
        }
    }
}
