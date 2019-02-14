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
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

// This class exists because DbCommand has non-abstract members
// that cannot be mocked, but need to be used. A specific example is
// DbCommand.Connection. This property needs to be used, but the getters
// and setters cannot be mocked. Instead, this class inherits from DbCommand
// allowing the base implementations to be invoked.
// REF: https://github.com/dotnet/corefx/blob/84bedcf58cfe951d37c5b3eba2957ddb2410f34d/src/System.Data.Common/src/System/Data/Common/DbCommand.cs#L30
namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public class DbCommandStub : DbCommand
    {
        private object _scalarResult = null;
        private int _nonQueryResult = 0;
        private DbDataReader _dbDataReader = null;
        private DbParameter _createDbParameterResult = null;

        // setup methods
        public void WithScalarResult(object _) => _scalarResult = _;
        public void WithNonQueryResult(int _) => _nonQueryResult = _;
        public void WithDbReader(DbDataReader _) => _dbDataReader = _;
        public void WithCreateDbParameterResult(DbParameter _) => _createDbParameterResult = _;

        // stubbed methods
        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; }
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        public override void Cancel() { }
        protected override DbParameter CreateDbParameter() => _createDbParameterResult;
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => _dbDataReader;

        public override int ExecuteNonQuery() => _nonQueryResult;
        public override object ExecuteScalar() => _scalarResult;
        public override void Prepare() { }
    }
}