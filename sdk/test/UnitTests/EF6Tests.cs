﻿//-----------------------------------------------------------------------------
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
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SQLite;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EF6Tests : TestBase
    {
        private SQLiteConnection connection = null;
        private AWSXRayRecorder recorder = null;
        private const string connectionString = "data source=:memory:";
        private const string database = "sqlite";
        private const string nameSpace = "remote";
        private const string commandText = "Test command text";

        [TestInitialize]
        public void Initialize()
        {
            recorder = new AWSXRayRecorder();
            recorder.BeginSegment("Test EF6");
            connection = new SQLiteConnection(connectionString);
            connection.Open();
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            connection.Close();
            connection.Dispose();
            recorder.EndSegment(); 
            recorder.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public void TestEFInterceptorNonQuery()
        {
            var efInterceptor = RegisterInterceptor(true);

            try
            {
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    DbInterception.Dispatch.Command.NonQuery(command, new DbCommandInterceptionContext()); // calling API from IDbCommandInterceptor
                }
            }
            catch
            {
                // Will throw exception as command text is invalid
            }

            var segment = recorder.GetEntity() as Segment;
            Assert.IsTrue(segment.Subsegments.Count != 0);

            var subsegment = segment.Subsegments[0];
            AssertTraceCollected(subsegment);
            Assert.AreEqual(commandText, subsegment.Sql["sanitized_query"]);

            RemoveInterceptor(efInterceptor);
        }

        [TestMethod]
        public void TestEFInterceptorReader()
        {
            var efInterceptor = RegisterInterceptor(true);

            try
            {
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    DbInterception.Dispatch.Command.Reader(command, new DbCommandInterceptionContext()); // calling API from IDbCommandInterceptor
                }
            }
            catch
            {
                // Will throw exception as command text is invalid
            }

            var segment = recorder.GetEntity() as Segment;
            Assert.IsTrue(segment.Subsegments.Count != 0);

            var subsegment = segment.Subsegments[0];
            AssertTraceCollected(subsegment);
            Assert.AreEqual(commandText, subsegment.Sql["sanitized_query"]);

            RemoveInterceptor(efInterceptor);
        }

        [TestMethod]
        public void TestEFInterceptorScalar()
        {
            var efInterceptor = RegisterInterceptor(true);

            try
            {
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    DbInterception.Dispatch.Command.Scalar(command, new DbCommandInterceptionContext()); // calling API from IDbCommandInterceptor
                }
            }
            catch
            {
                // Will throw exception as command text is invalid
            }

            var segment = recorder.GetEntity() as Segment;
            Assert.IsTrue(segment.Subsegments.Count != 0);

            var subsegment = segment.Subsegments[0];
            AssertTraceCollected(subsegment);
            Assert.AreEqual(commandText, subsegment.Sql["sanitized_query"]);

            RemoveInterceptor(efInterceptor);
        }

        [TestMethod]
        public void TestEFInterceptorNonQueryWithoutQueryText()
        {
            var efInterceptor = RegisterInterceptor(false);

            try
            {
                using (var command = new SQLiteCommand(commandText, connection))
                {
                    DbInterception.Dispatch.Command.NonQuery(command, new DbCommandInterceptionContext()); // calling API from IDbCommandInterceptor
                }
            }
            catch
            {
                // Will throw exception as command text is invalid
            }

            var segment = recorder.GetEntity() as Segment;
            Assert.IsTrue(segment.Subsegments.Count != 0);

            var subsegment = segment.Subsegments[0];
            AssertTraceCollected(subsegment);
            Assert.IsFalse(subsegment.Sql.ContainsKey("sanitized_query"));

            RemoveInterceptor(efInterceptor);
        }

        private EFInterceptor RegisterInterceptor(bool collectSqlQueries)
        {
            var efInterceptor = new EFInterceptor(collectSqlQueries);
            DbInterception.Add(efInterceptor);
            return efInterceptor;
        }

        // Remove EFInterceptor from DbInterceptor to avoid duplicate tracing
        private void RemoveInterceptor(EFInterceptor interceptor)
        {
            DbInterception.Remove(interceptor);
        }

        private void AssertTraceCollected(Subsegment subsegment)
        {
            Assert.AreEqual(connection.ConnectionString, subsegment.Sql["connection_string"]);
            Assert.AreEqual(database, subsegment.Sql["database_type"]);
            Assert.AreEqual(connection.ServerVersion, subsegment.Sql["database_version"]);
            Assert.AreEqual(nameSpace, subsegment.Namespace);
            Assert.IsTrue(subsegment.HasFault);
        }
    }
}
