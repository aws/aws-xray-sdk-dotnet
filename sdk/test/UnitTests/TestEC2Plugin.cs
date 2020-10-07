//-----------------------------------------------------------------------------
// <copyright file="TestPlugins.cs" company="Amazon.com">
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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.XRay.Recorder.Core.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class TestEC2Plugin
    {

        [TestMethod]
        public void TestV2Success()
        {
            // Arrange
            EC2Plugin ec2_plugin = new MockEC2Plugin(failV1: false, failV2: false);
            IDictionary<string, object> context = new Dictionary<string, object>();

            // Act
            bool ret = ec2_plugin.TryGetRuntimeContext(out context);

            // Assert
            Assert.IsTrue(ret);
            Assert.AreEqual(4, context.Count);
            
            object instance_id = "";
            context.TryGetValue("instance_id", out instance_id);
            Assert.AreEqual("i-07a181803de94c666", instance_id.ToString());

            object availability_zone = "";
            context.TryGetValue("availability_zone", out availability_zone);
            Assert.AreEqual("us-east-2a", availability_zone.ToString());

            object instance_size = "";
            context.TryGetValue("instance_size", out instance_size);
            Assert.AreEqual("t3.xlarge", instance_size.ToString());

            object ami_id = "";
            context.TryGetValue("ami_id", out ami_id);
            Assert.AreEqual("ami-03cca83dd001d4666", ami_id.ToString());
        }

        [TestMethod]
        public void TestV2Fail_V1Success()
        {
            // Arrange
            EC2Plugin ec2_plugin = new MockEC2Plugin(failV1: false, failV2: true);
            IDictionary<string, object> context = new Dictionary<string, object>();
            
            // Act
            bool ret = ec2_plugin.TryGetRuntimeContext(out context);

            // Assert
            Assert.IsTrue(ret);
            Assert.AreEqual(4, context.Count);
            object instance_id = "";
            context.TryGetValue("instance_id", out instance_id);
            Assert.AreEqual("i-07a181803de94c477", instance_id.ToString());

            object availability_zone = "";
            context.TryGetValue("availability_zone", out availability_zone);
            Assert.AreEqual("us-west-2a", availability_zone.ToString());

            object instance_size = "";
            context.TryGetValue("instance_size", out instance_size);
            Assert.AreEqual("t2.xlarge", instance_size.ToString());

            object ami_id = "";
            context.TryGetValue("ami_id", out ami_id);
            Assert.AreEqual("ami-03cca83dd001d4d11", ami_id.ToString());
        }

        [TestMethod]
        public void TestV2Fail_V1Fail()
        {
            // Arrange
            EC2Plugin ec2_plugin = new MockEC2Plugin(failV1: true, failV2: true);
            IDictionary<string, object> context = new Dictionary<string, object>();

            // Act
            bool ret = ec2_plugin.TryGetRuntimeContext(out context);

            // Assert
            Assert.IsFalse(ret);
            Assert.AreEqual(0, context.Count);
        }
    }

    // This is a mock class created for the purpose of unit testing. The overridden DoRequest method returns valid values or Exception 
    // based on the conditions for the tests. 
    public class MockEC2Plugin : EC2Plugin
    {
        private readonly bool _failV2;
        private readonly bool _failV1;

        public MockEC2Plugin(bool failV1, bool failV2)
        {
            _failV1 = failV1;
            _failV2 = failV2;
        }

        protected override Task<string> DoRequest(string url, HttpMethod method, Dictionary<string, string> headers = null)
        {
            if (_failV2 && url == "http://169.254.169.254/latest/api/token")
            {
                throw new Exception("Unable to complete the v2 request successfully");
            }
            else if (!_failV2 && url == "http://169.254.169.254/latest/api/token")
            {
                return Task.FromResult("dummyTokenfromferg");
            }
            else if (_failV1)
            {
                throw new Exception("Unable to complete the v1 request successfully");
            }
            
            string meta_string = "";
            if (headers == null) // for v1 endpoint request
            {
                meta_string = "{\"availabilityZone\" : \"us-west-2a\", \"imageId\" : \"ami-03cca83dd001d4d11\", \"instanceId\" : \"i-07a181803de94c477\", \"instanceType\" : \"t2.xlarge\"}";
                return Task.FromResult(meta_string);
            }
            else
            { // for v2 endpoint
                meta_string = "{\"availabilityZone\" : \"us-east-2a\", \"imageId\" : \"ami-03cca83dd001d4666\", \"instanceId\" : \"i-07a181803de94c666\", \"instanceType\" : \"t3.xlarge\"}";
                return Task.FromResult(meta_string);
            }
        }

    }
    
}
