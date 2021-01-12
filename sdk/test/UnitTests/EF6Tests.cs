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
using Amazon.XRay.Recorder.UnitTests.Tools;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SqlClient;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EF6Tests : TestBase
    {
        private SqlConnection connection = null;
        private const string connectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword";
        private const string connectionStringWithoutPassword = "server=myServerAddress;database=myDataBase;user id=myUsername";
        private const string subsegmentName = "myDataBase@myServerAddress";
        private const string userId = "myUsername";
        private const string database = "sqlserver";
        private const string nameSpace = "remote";
        private const string commandText = "Test command text";

        [TestInitialize]
        public void Initialize()
        {
            connection = new SqlConnection(connectionString);
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            connection.Close();
            connection.Dispose();
            base.TestCleanup();
        }

        [TestMethod]
        public void TestEFInterceptor()
        {
            AWSXRayRecorder.Instance.BeginSegment("Test EF6");

            AWSXRayEntityFramework6.AddXRayInterceptor(true);

            try
            {
                using (var SqlCommand = new SqlCommand(commandText, connection))
                {
                    DbInterception.Dispatch.Command.NonQuery(SqlCommand, new DbCommandInterceptionContext());
                }
            }
            catch
            {
                // Will throw exception as it's an invalid connection string

                var entity = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                Assert.IsTrue(entity is Subsegment);

                var subsegment = entity as Subsegment;
                Assert.AreEqual(subsegmentName, subsegment.Name);
                Assert.AreEqual(connectionStringWithoutPassword, subsegment.Sql["connection_string"]); // password will be removed
                Assert.AreEqual(database, subsegment.Sql["database_type"]);
                Assert.AreEqual(userId, subsegment.Sql["user"]);
                Assert.AreEqual(commandText, subsegment.Sql["sanitized_query"]);
                Assert.AreEqual(nameSpace, subsegment.Namespace);
                AWSXRayRecorder.Instance.EndSubsegment();
            }

            AWSXRayRecorder.Instance.EndSegment();
        }
    }
}
