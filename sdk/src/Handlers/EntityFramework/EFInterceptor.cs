//-----------------------------------------------------------------------------
// <copyright file="TraceableSqlCommand.net45.cs" company="Amazon.com">
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

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    public class EFInterceptor : DbCommandInterceptor
    {
        // Some database providers may not support Entity Framework Core 3.0 and above now
        // https://docs.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli
        private readonly string[] DatabaseTypes = { "mysql" , "sqlserver" , "sqlite" , "postgresql" , "firebird" , "cosmos" ,
                                                    "oracle" , "filecontextcore" , "jet" , "teradata" , "openedge" , "ibm" ,
                                                    "mycat" , "inmemory" };

        private const string SqlServerCompact35 = "sqlservercompact35";
        private const string SqlServerCompact40 = "sqlservercompact40";
        private const string DefaultDatabaseType = "EntityFrameworkCore";
        private readonly AWSXRayRecorder _recorder;
        private readonly bool? _collectSqlQueriesOverride;

        public EFInterceptor() : this(AWSXRayRecorder.Instance)
        {

        }

        public EFInterceptor(bool? collectSqlQueries = null) : this(AWSXRayRecorder.Instance, collectSqlQueries)
        {

        }

        public EFInterceptor(AWSXRayRecorder recorder, bool? collectSqlQueries = null) : base()
        {
            _recorder = recorder;
            _collectSqlQueriesOverride = collectSqlQueries;
        }

        /// <summary>
        /// Trace before executing reader.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <returns>Result from <see cref="IInterceptor"/>.</returns>
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ProcessBeginCommand(eventData);
            return base.ReaderExecuting(command, eventData, result);
        }

        /// <summary>
        /// Trace after executing reader.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Instance of <see cref="DbDataReader"/>.</param>
        /// <returns>Instance of <see cref="DbDataReader"/>.</returns>
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            ProcessEndCommand();
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <summary>
        /// Trace before executing reader asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(eventData);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Trace after executing reader asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result from <see cref="DbDataReader"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand();
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Trace after command fails.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandErrorEventData"/>.</param>
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            ProcessCommandError(eventData);
            base.CommandFailed(command, eventData);
        }

        /// <summary>
        /// Trace after async command fails.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandErrorEventData"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            ProcessCommandError(eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        /// <summary>
        /// Trace before excuting.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <returns>Task representing the operation.</returns>
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            ProcessBeginCommand(eventData);
            return base.NonQueryExecuting(command, eventData, result);
        }

        /// <summary>
        /// Trace before executing asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(eventData);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Trace after executing.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result as integer.</param>
        /// <returns>Result as integer.</returns>
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            ProcessEndCommand();
            return base.NonQueryExecuted(command, eventData, result);
        }

        /// <summary>
        /// Trace after executing asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result as integer.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand();
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Trace before executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <returns>Result from <see cref="IInterceptor"/>.</returns>
        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            ProcessBeginCommand(eventData);
            return base.ScalarExecuting(command, eventData, result);
        }

        /// <summary>
        /// Trace before executing scalar asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(eventData);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Trace after executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result object.</param>
        /// <returns>Result object.</returns>
        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            ProcessEndCommand();
            return base.ScalarExecuted(command, eventData, result);
        }

        /// <summary>
        /// Trace after executing scalar asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result object.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand();
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void ProcessBeginCommand(CommandEventData eventData)
        {
            Entity entity = null;
            try
            {
                entity = _recorder.GetEntity();
            }
            catch (EntityNotAvailableException e)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder, e, "Cannot get entity while processing start of Entity Framework command.");
            }

            _recorder.BeginSubsegment(BuildSubsegmentName(eventData.Command));
            _recorder.SetNamespace("remote");
            CollectSqlInformation(eventData);
        }

        private void ProcessEndCommand()
        {
            Entity entity = null;
            try
            {
                entity = _recorder.GetEntity();
            }
            catch (EntityNotAvailableException e)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder, e, "Cannot get entity while processing end of Entity Framework command.");
                return;
            }

            _recorder.EndSubsegment();
        }

        private void ProcessCommandError(CommandErrorEventData eventData)
        {
            Entity subsegment;
            try
            {
                subsegment = _recorder.GetEntity();
            }
            catch (EntityNotAvailableException e)
            {
                _recorder.TraceContext.HandleEntityMissing(_recorder, e, "Cannot get entity while processing failure of Entity Framework command.");
                return;
            }

            subsegment.AddException(eventData.Exception);

            _recorder.EndSubsegment();
        }

        /// <summary>
        /// Records the SQL information on the current subsegment,
        /// </summary>
        protected virtual void CollectSqlInformation(CommandEventData eventData)
        {
            // Get database type from DbContext
            string databaseType = GetDataBaseType(eventData.Context);
            _recorder.AddSqlInformation("database_type", databaseType);

            _recorder.AddSqlInformation("database_version", eventData.Command.Connection.ServerVersion);

            DbConnectionStringBuilder connectionStringBuilder = new DbConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = eventData.Command.Connection.ConnectionString;

            // Remove sensitive information from connection string
            connectionStringBuilder.Remove("Password");

            // Do a pre-check for UserID since in the case of TrustedConnection, a UserID may not be available.
            var user_id = GetConnectionValue(connectionStringBuilder, "User ID");
            if (user_id != null)
            {
                _recorder.AddSqlInformation("user", user_id.ToString());
            }

            _recorder.AddSqlInformation("connection_string", connectionStringBuilder.ToString());

            if (ShouldCollectSqlText())
            {
                _recorder.AddSqlInformation("sanitized_query", eventData.Command.CommandText);
            }
        }

        private string GetDataBaseType(DbContext context)
        {
            string databaseProvider = context.Database?.ProviderName?.ToLower();

            if (string.IsNullOrEmpty(databaseProvider))
            {
                return DefaultDatabaseType;
            }

            if (databaseProvider.Contains(SqlServerCompact35))
            {
                return SqlServerCompact35;
            }
            else if (databaseProvider.Contains(SqlServerCompact40))
            {
                return SqlServerCompact40;
            }

            string databaseType = null;

            foreach (string t in DatabaseTypes)
            {
                if (databaseProvider.Contains(t))
                {
                    databaseType = t;
                    break;
                }
            }

            return databaseType ?? databaseProvider;
        }

        private object GetConnectionValue(DbConnectionStringBuilder builder, string key)
        {
            object value = null;
            if (key == null)
            {
                return null;
            }
            builder.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Builds the name of the subsegment in the format database@datasource
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <returns>Returns the formed subsegment name as a string.</returns>
        private string BuildSubsegmentName(DbCommand command)
            => command.Connection.Database + "@" + RemovePortNumberFromDataSource(command.Connection.DataSource);

        private string RemovePortNumberFromDataSource(string dataSource)
        {
            Regex _portNumberRegex = new Regex(@",\d+$");
            return _portNumberRegex.Replace(dataSource, string.Empty);
        }

        private bool ShouldCollectSqlText()
            => _collectSqlQueriesOverride ?? _recorder.XRayOptions.CollectSqlQueries;
    }
}