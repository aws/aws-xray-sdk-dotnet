//-----------------------------------------------------------------------------
// <copyright file="EF6Tests.cs" company="Amazon.com">
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

using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Handlers.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SQLite;
using Amazon.XRay.Recorder.UnitTests.Tools;
using System.Linq;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EF6Tests : TestBase
    {
        private SQLiteConnection connection = null;
        private const string connectionString = "data source=:memory:";
        private const string database = "sqlite";
        private const string nameSpace = "remote";

        [TestInitialize]
        public void TestInitialize()
        {
            var sqliteConnectionStringBuilder = new SQLiteConnectionStringBuilder()
            {
                ConnectionString = connectionString,
            };

            connection = new SQLiteConnection(sqliteConnectionStringBuilder.ToString());
            connection.Open();
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            connection.Close();
            base.TestCleanup();
        }

        [TestMethod]
        public void TestEF6Interceptor()
        {
            AWSXRayRecorder.Instance.BeginSegment("Test EF6");

            AWSXRayEntityFramework6.AddXRayInterceptor(true); // enable tracing SQL query text

            try
            {
                using (var context = new TestEF6Context(connection, false))
                {
                    context.Users.ToList();
                    context.SaveChanges();
                }
            }
            catch
            {
                // EF6 will throw exception as there is no table Users in SQlite inmemory database, which will be recorded by X-Ray.
            }

            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity() as Segment;
            AWSXRayRecorder.Instance.EndSegment();

            Assert.IsTrue(segment.Subsegments.Count != 0);

            var subsegment = segment.Subsegments[0];
            Assert.AreEqual(connection.ConnectionString, subsegment.Sql["connection_string"]);
            Assert.AreEqual(database, subsegment.Sql["database_type"]);
            Assert.AreEqual(connection.ServerVersion, subsegment.Sql["database_version"]);
            Assert.IsNotNull(subsegment.Sql["sanitized_query"]);
            Assert.AreEqual(nameSpace, subsegment.Namespace);
            Assert.IsTrue(subsegment.HasFault);
        }
    }
}
