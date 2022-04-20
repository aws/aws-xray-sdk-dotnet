//-----------------------------------------------------------------------------
// <copyright file="EFInterceptor.net45.cs" company="Amazon.com">
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

using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    /// <summary>
    /// Class to intercept SQL query through EF 6 for .NET framework.
    /// </summary>
    public class EFInterceptor : IDbCommandInterceptor
    {
        private readonly bool? _collectSqlQueriesOverride;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFInterceptor" /> class.
        /// </summary>
        /// <param name="collectSqlQueries"></param>
        public EFInterceptor(bool? collectSqlQueries = null) : base()
        {
            _collectSqlQueriesOverride = collectSqlQueries;
        }

        /// <summary>
        /// Trace before executing non query.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            OnCommandStart(command);
        }

        /// <summary>
        /// Trace after executing non query.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            OnCommandStop(interceptionContext.Exception);
        }

        /// <summary>
        /// Trace before executing reader.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            OnCommandStart(command);
        }

        /// <summary>
        /// Trace after executing reader.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            OnCommandStop(interceptionContext.Exception);
        }

        /// <summary>
        /// Trace before executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            OnCommandStart(command);
        }

        /// <summary>
        /// Trace after executing scalar.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="interceptionContext">Instance of <see cref="DbCommandInterceptionContext"/>.</param>
        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            OnCommandStop(interceptionContext.Exception);
        }

        private void OnCommandStart(DbCommand command)
        {
            EFUtil.ProcessBeginCommand(command, _collectSqlQueriesOverride);
        }

        private static void OnCommandStop(Exception exception)
        {
            if (exception != null)
            {
                EFUtil.ProcessCommandError(exception);
            }
            else
            {
                EFUtil.ProcessEndCommand();
            }
        }
    }
}
