//-----------------------------------------------------------------------------
// <copyright file="EFInterceptor.cs" company="Amazon.com">
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
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    public class EFInterceptor : DbCommandInterceptor
    {
        private readonly bool? _collectSqlQueriesOverride;

        public EFInterceptor(bool? collectSqlQueries = null) : base()
        {
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
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
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
            EFUtil.ProcessEndCommand();
            return base.ReaderExecuted(command, eventData, result);
        }

#if NET6_0
        /// <summary>
        /// Trace after executing reader asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result from <see cref="DbDataReader"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
#endif

#if NET6_0
        /// <summary>
        /// Trace after executing reader asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result from <see cref="DbDataReader"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessEndCommand();
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessEndCommand();
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
#endif

        /// <summary>
        /// Trace after command fails.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandErrorEventData"/>.</param>
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            EFUtil.ProcessCommandError(eventData.Exception);
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
            EFUtil.ProcessCommandError(eventData.Exception);
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
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.NonQueryExecuting(command, eventData, result);
        }

#if NET6_0
        /// <summary>
        /// Trace before executing asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
#endif

        /// <summary>
        /// Trace after executing.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result as integer.</param>
        /// <returns>Result as integer.</returns>
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            EFUtil.ProcessEndCommand();
            return base.NonQueryExecuted(command, eventData, result);
        }

#if NET6_0
        /// <summary>
        /// Trace after executing asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result as integer.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessEndCommand();
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessEndCommand();
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
#endif

        /// <summary>
        /// Trace before executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <returns>Result from <see cref="IInterceptor"/>.</returns>
        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.ScalarExecuting(command, eventData, result);
        }

#if NET6_0
        /// <summary>
        /// Trace before executing scalar asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandEventData"/>.</param>
        /// <param name="result">Result from <see cref="IInterceptor"/>.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
#endif

        /// <summary>
        /// Trace after executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result object.</param>
        /// <returns>Result object.</returns>
        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            EFUtil.ProcessEndCommand();
            return base.ScalarExecuted(command, eventData, result);
        }

#if NET6_0
        /// <summary>
        /// Trace after executing scalar asynchronously.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="eventData">Instance of <see cref="CommandExecutedEventData"/>.</param>
        /// <param name="result">Result object.</param>
        /// <param name="cancellationToken">Instance of <see cref="CancellationToken"/>.</param>
        /// <returns>Task representing the async operation.</returns>
        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            EFUtil.ProcessEndCommand();
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }
#else
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
            EFUtil.ProcessEndCommand();
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }
#endif
    }
}