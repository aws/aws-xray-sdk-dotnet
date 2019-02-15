//-----------------------------------------------------------------------------
// <copyright file="DbCommandInterceptorTests.cs" company="Amazon.com">
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
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Amazon.XRay.Recorder.Handlers.SqlServer;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.UnitTests.Tools;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class DbCommandInterceptorTests : TestBase
    {
        private DbCommandStub _command = new DbCommandStub();
        private const string _userId = "admin";
        private const string _connectionString = "Data Source=xyz.com,3306;User ID=" + _userId + ";Password=Secret.123;";
        private const string _sanitizedConnectionString = "Data Source=xyz.com,3306;User ID=" + _userId;

        [TestInitialize]
        public void TestInitialize() 
        {
            var connectionMock = new Mock<DbConnection>();
            connectionMock.Setup(c => c.DataSource).Returns("xyz.com,3306");
            connectionMock.Setup(c => c.Database).Returns("master");
            connectionMock.Setup(c => c.ServerVersion).Returns("13.0.5026.0");
            connectionMock.Setup(c => c.ConnectionString).Returns(_connectionString);

            _command.Connection = connectionMock.Object;
            _command.CommandText = "SELECT a.* FROM dbo.Accounts a ...";
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            AWSXRayRecorder.Instance.Dispose();
        }

        [TestMethod]
        public void Intercept_DoesNot_CollectQueries_When_NotEnabled()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions()
            };
            recorder.BeginSegment("test");
            var interceptor = new DbCommandInterceptor(recorder);

            // act
            interceptor.Intercept(() => 0, _command);

            // assert
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            
            AssertNotCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public async Task InterceptAsync_DoesNot_CollectQueries_When_NotEnabled()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions()
            };
            recorder.BeginSegment("test");
            var interceptor = new DbCommandInterceptor(recorder);

            // act
            await interceptor.InterceptAsync(() => Task.FromResult(0), _command);

            // assert
            var segment = AWSXRayRecorder.Instance.TraceContext.GetEntity();
            
            AssertNotCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public void Intercept_CollectsQueries_When_DisabledGlobally_And_EnabledLocally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = false }
            };
            recorder.BeginSegment("test");
            var interceptor = new DbCommandInterceptor(recorder, collectSqlQueries: true);

            // act            
            interceptor.Intercept(() => 0, _command);

            // assert
            AssertCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public async Task InterceptAsync_CollectsQueries_When_DisabledGlobally_And_EnabledLocally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = false }
            };
            recorder.BeginSegment("test");
            var interceptor = new DbCommandInterceptor(recorder, collectSqlQueries: true);

            // act            
            await interceptor.InterceptAsync(() => Task.FromResult(0), _command);

            // assert
            AssertCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public void Intercept_CollectsQueries_When_EnabledGlobally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = true }
            };
            var interceptor = new DbCommandInterceptor(recorder);
            recorder.BeginSegment("test");

            // act
            interceptor.Intercept(() => 0, _command);

            // assert
            AssertCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public async Task InterceptAsync_CollectsQueries_When_EnabledGlobally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = true }
            };
            var interceptor = new DbCommandInterceptor(recorder);
            recorder.BeginSegment("test");

            // act
            await interceptor.InterceptAsync(() => Task.FromResult(0), _command);

            // assert
            AssertCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public void Intercept_DoesNot_CollectQueries_When_EnabledGlobally_And_DisabledLocally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = true }
            };
            var interceptor = new DbCommandInterceptor(recorder, collectSqlQueries: false);
            recorder.BeginSegment("test");

            // act
            interceptor.Intercept(() => 0, _command);

            // assert
            AssertNotCollected(recorder);
            recorder.EndSegment();
        }

        [TestMethod]
        public async Task InterceptAsync_DoesNot_CollectQueries_When_EnabledGlobally_And_DisabledLocally()
        {
            // arrange
            var recorder = new AWSXRayRecorder {
                XRayOptions = new XRayOptions { CollectSqlQueries = true }
            };
            var interceptor = new DbCommandInterceptor(recorder, collectSqlQueries: false);
            recorder.BeginSegment("test");

            // act
            await interceptor.InterceptAsync(() => Task.FromResult(0), _command);

            // assert
            AssertNotCollected(recorder);
            recorder.EndSegment();
        }

        private void AssertNotCollected(AWSXRayRecorder recorder)
        {
            var segment = recorder.TraceContext.GetEntity().Subsegments[0];
            
            AssertExpectedSqlInformation(segment);

            Assert.AreEqual(4, segment.Sql.Count);
            Assert.IsFalse(segment.Sql.ContainsKey("sanitized_query"));
        }

        private void AssertCollected(AWSXRayRecorder recorder)
        {
            var segment = recorder.TraceContext.GetEntity().Subsegments[0];
            
            AssertExpectedSqlInformation(segment);

            Assert.AreEqual(5, segment.Sql.Count);            
            Assert.AreEqual(_command.CommandText, segment.Sql["sanitized_query"]);
        }

        private void AssertExpectedSqlInformation(Subsegment segment)
        {
            Assert.IsNotNull(segment);
            Assert.IsNotNull(segment.Sql);
            Assert.AreEqual("sqlserver", segment.Sql["database_type"]);
            Assert.AreEqual(_command.Connection.ServerVersion, segment.Sql["database_version"]);
            Assert.AreEqual(_userId, segment.Sql["user"]);
            Assert.AreEqual(_sanitizedConnectionString, segment.Sql["connection_string"]);
        }
    }
}