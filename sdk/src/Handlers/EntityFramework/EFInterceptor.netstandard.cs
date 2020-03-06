using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Exceptions;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    public class EFInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        private const string DataBaseTypeString = "sqlserver";
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

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ProcessBeginCommand(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            ProcessEndCommand(command);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            ProcessCommandError(command, eventData);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            ProcessCommandError(command, eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            ProcessBeginCommand(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            ProcessEndCommand(command);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            ProcessBeginCommand(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            ProcessBeginCommand(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            ProcessEndCommand(command);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            ProcessEndCommand(command);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void ProcessBeginCommand(DbCommand command)
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

            //_recorder.BeginSubsegment(SqlUtil.BuildSubsegmentName(command)); //commented because its failing with invalid name due to \\
            _recorder.BeginSubsegment("EF_sub");
            _recorder.SetNamespace("remote");
            CollectSqlInformation(command);
        }

        private void ProcessEndCommand(DbCommand command)
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

            //CollectSqlInformation(command); //moving to processbegincommand so that errors would also have sql info.
            _recorder.EndSubsegment();
        }

        private void ProcessCommandError(DbCommand command, CommandErrorEventData eventData)
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
        protected virtual void CollectSqlInformation(DbCommand command)
        {
            _recorder.AddSqlInformation("database_type", DataBaseTypeString);

            _recorder.AddSqlInformation("database_version", command.Connection.ServerVersion);

            //SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(command.Connection.ConnectionString);

            DbConnectionStringBuilder connectionStringBuilder = new DbConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = command.Connection.ConnectionString;

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
                _recorder.AddSqlInformation("sanitized_query", command.CommandText);
            }
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

#if !NET45
        private bool ShouldCollectSqlText()
            => _collectSqlQueriesOverride ?? _recorder.XRayOptions.CollectSqlQueries;
#else
        private bool ShouldCollectSqlText()
            => _collectSqlQueriesOverride ?? AppSettings.CollectSqlQueries;
#endif
    }
}
