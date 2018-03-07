//-----------------------------------------------------------------------------
// <copyright file="JsonSegmentMarshallerTest.cs" company="Amazon.com">
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Amazon.DynamoDBv2;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ThirdParty.LitJson;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class JsonSegmentMarshallerTest
    {
        private JsonSegmentMarshaller _marshaller;

        [TestInitialize]
        public void Initialize()
        {
            _marshaller = new JsonSegmentMarshaller();
        }

        [TestMethod]
        public void TestMarshallHttpMethod()
        {
            var segment = new Segment("test", "1-11111111-111111111111111111111111");
            segment.Id = "1111111111111111";
            segment.StartTime = 1;
            segment.EndTime = 2;
            
            // ensure method can be marshalled
            segment.Http["method"] = HttpMethod.Post;
            
            
            _marshaller.Marshall(segment);

            var expect = "{\"format\":\"json\",\"version\":1}\n{\"trace_id\":\"1-11111111-111111111111111111111111\",\"id\":\"1111111111111111\",\"start_time\":1,\"end_time\":2,\"name\":\"test\",\"http\":{\"method\":\"POST\"}}";
            var actual = _marshaller.Marshall(segment);
            
            Assert.AreEqual(expect, actual);
        }
        
        [TestMethod]
        public void TestMarshallConstantClass()
        {
            var segment = new Segment("test", "1-11111111-111111111111111111111111");
            segment.Id = "1111111111111111";
            segment.StartTime = 1;
            segment.EndTime = 2;
            
            // Some random instance of ConstantClass. Their constructor is private so we cant construct one
            segment.Aws["value"] = ReturnConsumedCapacity.INDEXES;
            
            
            _marshaller.Marshall(segment);

            var expect = "{\"format\":\"json\",\"version\":1}\n{\"trace_id\":\"1-11111111-111111111111111111111111\",\"id\":\"1111111111111111\",\"start_time\":1,\"end_time\":2,\"name\":\"test\",\"aws\":{\"value\":\"INDEXES\"}}";
            var actual = _marshaller.Marshall(segment);
            
            Assert.AreEqual(expect, actual);
        }

        [TestMethod]
        public void TestMarshallSimpleSegment()
        {
            var segment = new Segment("test", "1-11111111-111111111111111111111111");
            segment.Id = "1111111111111111";
            segment.StartTime = 100;
            segment.EndTime = 200;

            var expect = "{\"format\":\"json\",\"version\":1}\n{\"trace_id\":\"1-11111111-111111111111111111111111\",\"id\":\"1111111111111111\",\"start_time\":100,\"end_time\":200,\"name\":\"test\"}";
            var actual = _marshaller.Marshall(segment);

            Assert.AreEqual(expect, actual);
        }

        [TestMethod]
        public void TestMarshallSubsegment()
        {
            var parent = new Segment("parent", "1-11111111-111111111111111111111111");
            parent.Id = "1111111111111111";
            parent.StartTime = 100;
            parent.EndTime = 400;

            var child = new Subsegment("child");
            child.Id = "2222222222222222";
            child.StartTime = 200;
            child.EndTime = 300;

            parent.AddSubsegment(child);

            var expect = "{\"format\":\"json\",\"version\":1}\n{\"trace_id\":\"1-11111111-111111111111111111111111\",\"id\":\"1111111111111111\",\"start_time\":100,\"end_time\":400,\"name\":\"parent\",\"subsegments\":[{\"id\":\"2222222222222222\",\"start_time\":200,\"end_time\":300,\"name\":\"child\"}]}";
            var actual = _marshaller.Marshall(parent);

            Assert.AreEqual(actual, expect);
        }

        [TestMethod]
        public void TestMarshallSegmentWithAws()
        {
            var segment = new Segment("test", "1-11111111-111111111111111111111111");
            segment.Id = "1111111111111111";
            segment.StartTime = 100;
            segment.EndTime = 200;

            segment.Aws["region"] = "us-east-1";
            segment.Aws["operation"] = "ListTablesRequest";
            segment.Aws["request_id"] = "123456";

            var actual = _marshaller.Marshall(segment);
            var actualJson = JsonMapper.ToObject(actual.Split('\n')[1]);

            Assert.AreEqual("us-east-1", (string)actualJson["aws"]["region"]);
            Assert.AreEqual("ListTablesRequest", (string)actualJson["aws"]["operation"]);
            Assert.AreEqual("123456", (string)actualJson["aws"]["request_id"]);
        }

        [TestMethod]
        public void TestMarshallAddException()
        {
            try
            {
                throw new ArgumentNullException("value");
            }
            catch (ArgumentNullException e)
            {
                var subsegment = new Subsegment("test");
                subsegment.Id = "1111111111111111";
                subsegment.AddException(e);

                var actual = _marshaller.Marshall(subsegment);

                var trace = new StackTrace(true);
                var filePath = trace.GetFrame(0).GetFileName().Replace("\\", "\\\\");
                var line = new StackTrace(e, true).GetFrame(0).GetFileLineNumber();
                var workingDirectory = Directory.GetCurrentDirectory().Replace("\\", "\\\\");
                var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"fault\":true,\"cause\":{\"working_directory\":\"" + workingDirectory + "\",\"exceptions\":[{\"id\":\"" + subsegment.Cause.ExceptionDescriptors[0].Id + "\",\"message\":\"Value cannot be null.\\r\\nParameter name: value\",\"type\":\"ArgumentNullException\",\"stack\":[{\"path\":\"" + filePath + "\",\"line\":" + line + ",\"label\":\"Amazon.XRay.Recorder.UnitTests.JsonSegmentMarshallerTest.TestMarshallAddException\"}]}]}}";

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void TestMarshallError()
        {
            try
            {
                throw new ArgumentNullException("value");
            }
            catch (ArgumentNullException)
            {
                var subsegment = new Subsegment("test");
                subsegment.Id = "1111111111111111";
                subsegment.HasError = true;

                var actual = _marshaller.Marshall(subsegment);
                var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"error\":true}";

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void TestMarshallAnnotation()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            subsegment.AddAnnotation("string", "US");
            subsegment.AddAnnotation("bool", true);
            subsegment.AddAnnotation("int", 98177);
            subsegment.AddAnnotation("long", 123456789123456L);
            subsegment.AddAnnotation("double", 100.256);

            var actual = _marshaller.Marshall(subsegment);
            var actualJson = JsonMapper.ToObject(actual.Split('\n')[1]);

            Assert.AreEqual("US", (string)actualJson["annotations"]["string"]);
            Assert.AreEqual(true, (bool)actualJson["annotations"]["bool"]);
            Assert.AreEqual(98177, (int)actualJson["annotations"]["int"]);
            Assert.AreEqual(123456789123456L, (long)actualJson["annotations"]["long"]);
            Assert.AreEqual(100.256, (double)actualJson["annotations"]["double"]);
        }

        [TestMethod]
        public void TestMarshallHttp()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            var request = new Dictionary<string, string>();
            request["url"] = @"http://hello-1.mbfzqxzcpe.us-east-1.elasticbeanstalk.com/foo";
            request["method"] = "GET";

            subsegment.Http["request"] = request;

            var actual = _marshaller.Marshall(subsegment);
            var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"http\":{\"request\":{\"url\":\"http://hello-1.mbfzqxzcpe.us-east-1.elasticbeanstalk.com/foo\",\"method\":\"GET\"}}}";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallNamespace()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            subsegment.Namespace = "remote";

            var actual = _marshaller.Marshall(subsegment);
            var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"namespace\":\"remote\"}";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallThrottle()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            subsegment.IsThrottled = true;

            var actual = _marshaller.Marshall(subsegment);
            var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"throttle\":true}";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallPrecursorIds()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            subsegment.AddPrecursorId("2222222222222222");
            subsegment.AddPrecursorId("2222222222222222");
            subsegment.AddPrecursorId("3333333333333333");

            var actual = _marshaller.Marshall(subsegment);
            var expected = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"precursor_ids\":[\"2222222222222222\",\"3333333333333333\"]}";

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestMarshallSql()
        {
            var subsegment = new Subsegment("test");
            subsegment.Id = "1111111111111111";
            subsegment.Sql["key1"] = "value1";
            subsegment.Sql["key2"] = "value2";

            var actual = _marshaller.Marshall(subsegment);
            var expected1 = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"sql\":{\"key2\":\"value2\",\"key1\":\"value1\"}}";
            var expected2 = "{\"format\":\"json\",\"version\":1}\n{\"id\":\"1111111111111111\",\"start_time\":0,\"end_time\":0,\"name\":\"test\",\"sql\":{\"key1\":\"value1\",\"key2\":\"value2\"}}";

            bool result = false;
            if (Object.Equals(expected1, actual) || Object.Equals(expected2, actual))
            {
                result = true;
            }
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestMarshallMetadata()
        {
            var subsegment = new Subsegment("metadata");
            subsegment.Id = "1111111111111111";
            subsegment.AddMetadata("key1", "value1");
            subsegment.AddMetadata("key2", "value2");
            subsegment.AddMetadata("aws", "key1", "value1");

            var actual = _marshaller.Marshall(subsegment);
            var actualJson = JsonMapper.ToObject(actual.Split('\n')[1]);

            Assert.AreEqual("value1", (string)actualJson["metadata"]["default"]["key1"]);
            Assert.AreEqual("value2", (string)actualJson["metadata"]["default"]["key2"]);
            Assert.AreEqual("value1", (string)actualJson["metadata"]["aws"]["key1"]);
        }

        [TestMethod]
        public void TestMarshallServiceContext()
        {
            var segment = new Segment("service", "1-11111111-111111111111111111111111");
            segment.Service["key"] = "value";
            segment.Id = "1111111111111111";
            segment.StartTime = 100;
            segment.EndTime = 200;

            var expect = "{\"format\":\"json\",\"version\":1}\n{\"trace_id\":\"1-11111111-111111111111111111111111\",\"id\":\"1111111111111111\",\"start_time\":100,\"end_time\":200,\"name\":\"service\",\"service\":{\"key\":\"value\"}}";
            var actual = _marshaller.Marshall(segment);

            Assert.AreEqual(actual, expect);
        }
    }
}
