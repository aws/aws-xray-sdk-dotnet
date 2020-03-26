//-----------------------------------------------------------------------------
// <copyright file="EFUtilTests.cs" company="Amazon.com">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Amazon.XRay.Recorder.Handlers.EntityFramework;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class EFUtilTests : TestBase
    {
        private string sqlServerConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;port=1234;";
        private string mySqlConnectionString = "server=localhost;database=temp;user=root;pwd=1234abcD*;port=1234;";
        private string sqliteConnectionString = "Data Source=/ex1.db;port=1234;";
        private string postgreSqlConnectionString = "Host=localhost;Database=postgres;Userid=postgres;password=1234abcD*;Port=1234;";
        private string firebirdSqlConnectionString = "Username=SYSDBA;Password=masterkey;Database=/firebird.fdb;DataSource=localhost;port=1234;";

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
        }

        [TestMethod]
        public void Test_Get_UserId_MySql()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
            {
                ConnectionString = mySqlConnectionString
            };
            object result = EFUtil.GetConnectionValue(builder);
            Assert.AreEqual("root", result.ToString());
        }

        [TestMethod]
        public void Test_Get_UserId_SqlServer()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
            {
                ConnectionString = sqlServerConnectionString
            };
            object result = EFUtil.GetConnectionValue(builder);
            Assert.AreEqual("myUsername", result.ToString());
        }

        [TestMethod]
        public void Test_Get_UserId_Sqlite()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
            {
                ConnectionString = sqliteConnectionString
            };
            object result = EFUtil.GetConnectionValue(builder);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Test_Get_UserId_PostgreSql()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
            {
                ConnectionString = postgreSqlConnectionString
            };
            object result = EFUtil.GetConnectionValue(builder);
            Assert.AreEqual("postgres", result.ToString());
        }

        [TestMethod]
        public void Test_Get_UserId_FirebirdSql()
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
            {
                ConnectionString = firebirdSqlConnectionString
            };
            object result = EFUtil.GetConnectionValue(builder);
            Assert.AreEqual("SYSDBA", result.ToString());
        }
    }
}