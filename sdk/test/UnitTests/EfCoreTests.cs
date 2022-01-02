//-----------------------------------------------------------------------------
// <copyright file="EFCoreTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.UnitTests.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Amazon.XRay.Recorder.Core;
using System.Linq;
using Microsoft.Data.Sqlite;
using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EfCoreTests : TestBase
    {
        private const String _connectionString = "datasource=:memory:";
        private const String _queryString = "SELECT \"u\".\"UserId\"\r\nFROM \"Users\" AS \"u\"\r\nWHERE \"u\".\"UserId\" = 1";
        private const String _dbVersion = "3.28.0";
        private SqliteConnection connection = null;

        [TestInitialize]
        public void TestInitialize()
        {
            // In-memory database only exists while the connection is open
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            connection.Close();
            base.TestCleanup();
        }

        [TestMethod]
        public void Test_EFCore_successful_query()
        {
            // Arrange
            var recorder = new AWSXRayRecorder();

            recorder.BeginSegment("TestSegment");
            var context = GetTestEFContext(true);
            // Act
            var users = context.Users.Where(u => u.UserId == 1).ToList();

            // Assert
            var segment = recorder.TraceContext.GetEntity();
            Assert.AreEqual(3, segment.Subsegments.Count); //2 subsegments from validating the db and 1 from the actual query
            var query_subsegment = segment.Subsegments[2];
            AssertQueryCollected(query_subsegment);

            recorder.EndSegment();
        }

        [TestMethod]
        public void Test_EFCore_unsuccessful_query()
        {
            var recorder = new AWSXRayRecorder();

            recorder.BeginSegment("TestSegment");
            var context = GetTestEFContext(false);
            var users = context.Users.Where(u => u.UserId == 1).ToList();

            var segment = recorder.TraceContext.GetEntity();
            Assert.AreEqual(3, segment.Subsegments.Count);
            var query_subsegment = segment.Subsegments[2];
            AssertQueryNotCollected(query_subsegment);
            recorder.EndSegment();
        }

        [TestMethod]
        public void Test_EFCore_query_with_exception()
        {
            var recorder = new AWSXRayRecorder();

            recorder.BeginSegment("TestSegment");
            var context = GetTestEFContext(true);

            try
            {
#if NET6_0
                context.Database.ExecuteSqlInterpolated($"Select * From FakeTable"); // A false sql command which results in 'no such table: FakeTable' exception
#else
                context.Database.ExecuteSqlCommand("Select * From FakeTable"); // A false sql command which results in 'no such table: FakeTable' exception
#endif
            }
            catch
            {
                // ignore
            }

            var segment = recorder.TraceContext.GetEntity();
            Assert.AreEqual(3, segment.Subsegments.Count);
            var subsegment = segment.Subsegments[2];
            Assert.AreEqual(true, subsegment.HasFault);
            recorder.EndSegment();
        }

        private void AssertQueryCollected(Subsegment subsegment)
        {
            AssertExpectedSqlInformation(subsegment);
            Assert.IsTrue(subsegment.Sql.ContainsKey("sanitized_query"));
        }

        private void AssertQueryNotCollected(Subsegment subsegment)
        {
            AssertExpectedSqlInformation(subsegment);
            Assert.IsFalse(subsegment.Sql.ContainsKey("sanitized_query"));
        }

        private void AssertExpectedSqlInformation(Subsegment subsegment)
        {
            Assert.IsNotNull(subsegment);
            Assert.IsNotNull(subsegment.Sql);
            Assert.AreEqual(_connectionString, subsegment.Sql["connection_string"]);
            Assert.AreEqual(connection.ServerVersion, subsegment.Sql["database_version"]);
        }

        private TestEFContext GetTestEFContext(bool collectSqlQueries)
        {
            var options = new DbContextOptionsBuilder<TestEFContext>()
                .UseSqlite(connection)
                .AddXRayInterceptor(collectSqlQueries)
                .Options;
            var context = new TestEFContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
