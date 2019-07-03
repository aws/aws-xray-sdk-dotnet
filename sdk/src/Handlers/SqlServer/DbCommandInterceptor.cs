//-----------------------------------------------------------------------------
// <copyright file="DbCommandInterceptor.cs" company="Amazon.com">
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Handlers.SqlServer
{
    /// <summary>
    /// Intercepts DbCommands and records them in new Subsegments.
    /// </summary>
    public interface IDbCommandInterceptor
    {
        /// <summary>
        /// Begins a new Subsegment, executes the provided async operation,
        /// and records the request in the "sql" member of the subsegment.
        /// </summary>
        /// <example>
        /// <code>
        /// await InterceptAsync(() => dbCommand.ExecuteNonQueryAsync(cancellationToken), dbCommand);
        /// </code>
        /// </example>
        Task<TResult> InterceptAsync<TResult>(Func<Task<TResult>> method, DbCommand command);

        /// <summary>
        /// Begins a new Subsegment, executes the provided operation,
        /// and records the request in the "sql" member of the subsegment.
        /// </summary>
        /// <example>
        /// <code>
        /// await Intercept(() => dbCommand.ExecuteNonQuery(), dbCommand);
        /// </code>
        /// </example>
        TResult Intercept<TResult>(Func<TResult> method, DbCommand command);
    }

    /// <inheritdoc />
    public class DbCommandInterceptor : IDbCommandInterceptor
    {
        private const string DataBaseTypeString = "sqlserver";
        private readonly AWSXRayRecorder _recorder;
        private readonly bool? _collectSqlQueriesOverride;        

        public DbCommandInterceptor(AWSXRayRecorder recorder, bool? collectSqlQueries = null)
        {
            _recorder = recorder;
            _collectSqlQueriesOverride = collectSqlQueries;
        }

        /// <inheritdoc />
        public async Task<TResult> InterceptAsync<TResult>(Func<Task<TResult>> method, DbCommand command)
        {
            _recorder.BeginSubsegment(BuildSubsegmentName(command));
            try
            {
                _recorder.SetNamespace("remote");
                var ret = await method();
                CollectSqlInformation(command);

                return ret;
            }
            catch (Exception e)
            {
                _recorder.AddException(e);
                throw;
            }
            finally
            {
                _recorder.EndSubsegment();
            }
        }

        /// <inheritdoc />
        public TResult Intercept<TResult>(Func<TResult> method, DbCommand command)
        {
            _recorder.BeginSubsegment(BuildSubsegmentName(command));
            try
            {
                _recorder.SetNamespace("remote");
                var ret = method();
                CollectSqlInformation(command);

                return ret;
            }
            catch (Exception e)
            {
                _recorder.AddException(e);
                throw;
            }
            finally
            {
                _recorder.EndSubsegment();
            }
        }

        /// <summary>
        /// Records the SQL information on the current subsegment,
        /// </summary>
        protected virtual void CollectSqlInformation(DbCommand command)
        {
            _recorder.AddSqlInformation("database_type", DataBaseTypeString);

            _recorder.AddSqlInformation("database_version", command.Connection.ServerVersion);

            SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(command.Connection.ConnectionString);

            // Remove sensitive information from connection string
            connectionStringBuilder.Remove("Password");

            _recorder.AddSqlInformation("user", connectionStringBuilder.UserID);
            _recorder.AddSqlInformation("connection_string", connectionStringBuilder.ToString());

            if(ShouldCollectSqlText()) 
            {
                _recorder.AddSqlInformation("sanitized_query", command.CommandText);
            }
        }

        /// <summary>
        /// Builds the name of the subsegment in the format database@datasource
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Returns the formed subsegment name as a string.</returns>
        private string BuildSubsegmentName(DbCommand command) 
            => command.Connection.Database + "@" + SqlUtil.RemovePortNumberFromDataSource(command.Connection.DataSource);

#if !NET45
        private bool ShouldCollectSqlText() 
            => _collectSqlQueriesOverride ?? _recorder.XRayOptions.CollectSqlQueries;
#else
        private bool ShouldCollectSqlText()
            => _collectSqlQueriesOverride ?? AppSettings.CollectSqlQueries;
#endif
    }
}