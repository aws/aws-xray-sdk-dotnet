﻿//-----------------------------------------------------------------------------
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

using System;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Handlers.SqlServer
{
    /// <summary>
    /// Traceable wrapper of <see cref="SqlCommand"/>. Currently  synchronized and asynchronized call
    /// are traced, which includes ExecuteNonQuery, ExecuteReader, ExecuteScalar and ExecuteXmlReader.
    /// </summary>
    /// <seealso cref="SqlCommand" />
    public class TraceableSqlCommand : DbCommand, ICloneable
    {
        private const string DataBaseTypeString = "sqlserver";

        private IDbCommandInterceptor _interceptor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceableSqlCommand"/> class.
        /// </summary>
        /// <param name="collectSqlQueries">
        /// Include the <see cref="TraceableSqlCommand.CommandText" /> in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are including sensitive information as clear text.
        /// This flag will override any behavior configured by <see cref="AppSettings.CollectSqlQueries" />.
        /// If a value is not provided, then the globally configured value will be used, which is false by default.
        /// See the official documentation on <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters?view=netframework-4.7.2">SqlCommand.Parameters</a>
        /// </param>
        public TraceableSqlCommand(bool? collectSqlQueries = null)
        {
            InnerSqlCommand = new SqlCommand();
            _interceptor = new DbCommandInterceptor(AWSXRayRecorder.Instance, collectSqlQueries);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceableSqlCommand"/> class.
        /// </summary>
        /// <param name="cmdText">The text of the query.</param>
        /// <param name="collectSqlQueries">
        /// Include the <see cref="TraceableSqlCommand.CommandText" /> in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are including sensitive information as clear text.
        /// This flag will override any behavior configured by <see cref="AppSettings.CollectSqlQueries" />.
        /// If a value is not provided, then the globally configured value will be used, which is false by default.
        /// See the official documentation on <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters?view=netframework-4.7.2">SqlCommand.Parameters</a>
        /// </param>
        public TraceableSqlCommand(string cmdText, bool? collectSqlQueries = null)
        {
            InnerSqlCommand = new SqlCommand(cmdText);
            _interceptor = new DbCommandInterceptor(AWSXRayRecorder.Instance, collectSqlQueries);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceableSqlCommand"/> class.
        /// </summary>
        /// <param name="cmdText">The text of the query.</param>
        /// <param name="connection">The connection to an instance of SQL Server.</param>
        /// <param name="collectSqlQueries">
        /// Include the <see cref="TraceableSqlCommand.CommandText" /> in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are including sensitive information as clear text.
        /// This flag will override any behavior configured by <see cref="AppSettings.CollectSqlQueries" />.
        /// If a value is not provided, then the globally configured value will be used, which is false by default.
        /// See the official documentation on <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters?view=netframework-4.7.2">SqlCommand.Parameters</a>
        /// </param>
        public TraceableSqlCommand(string cmdText, SqlConnection connection, bool? collectSqlQueries = null)
        {
            InnerSqlCommand = new SqlCommand(cmdText, connection);
            _interceptor = new DbCommandInterceptor(AWSXRayRecorder.Instance, collectSqlQueries);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceableSqlCommand"/> class.
        /// </summary>
        /// <param name="cmdText">The text of the query.</param>
        /// <param name="connection">The connection to an instance of SQL Server.</param>
        /// <param name="transaction">The <see cref="SqlTransaction"/> in which the <see cref="SqlCommand"/> executes.</param>
        /// <param name="collectSqlQueries">
        /// Include the <see cref="TraceableSqlCommand.CommandText" /> in the sanitized_query section of 
        /// the SQL subsegment. Parameterized values will appear in their tokenized form and will not be expanded.
        /// You should not enable this flag if you are including sensitive information as clear text.
        /// This flag will override any behavior configured by <see cref="AppSettings.CollectSqlQueries" />.
        /// If a value is not provided, then the globally configured value will be used, which is false by default.
        /// See the official documentation on <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.parameters?view=netframework-4.7.2">SqlCommand.Parameters</a>
        /// </param>
        public TraceableSqlCommand(string cmdText, SqlConnection connection, SqlTransaction transaction, bool? collectSqlQueries = null)
        {
            InnerSqlCommand = new SqlCommand(cmdText, connection, transaction);
            _interceptor = new DbCommandInterceptor(AWSXRayRecorder.Instance, collectSqlQueries);
        }

        private TraceableSqlCommand(TraceableSqlCommand from)
        {
            InnerSqlCommand = from.InnerSqlCommand.Clone();
        }

        /// <summary>
        /// Occurs when the execution of a Transact-SQL statement completes.
        /// </summary>
        public event StatementCompletedEventHandler StatementCompleted
        {
            add { InnerSqlCommand.StatementCompleted += value; }
            remove { InnerSqlCommand.StatementCompleted -= value; }
        }

        /// <summary>
        /// Gets the inner SQL command.
        /// </summary>
        public SqlCommand InnerSqlCommand { get; private set; }

        /// <summary>
        /// Gets or sets the text command to run against the data source.
        /// </summary>
        public override string CommandText
        {
            get { return InnerSqlCommand.CommandText; }
            set { InnerSqlCommand.CommandText = value; }
        }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public override int CommandTimeout
        {
            get { return InnerSqlCommand.CommandTimeout; }
            set { InnerSqlCommand.CommandTimeout = value; }
        }

        /// <summary>
        /// Indicates or specifies how the <see cref="P:System.Data.Common.DbCommand.CommandText" /> property is interpreted.
        /// </summary>
        public override CommandType CommandType
        {
            get { return InnerSqlCommand.CommandType; }
            set { InnerSqlCommand.CommandType = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.Common.DbConnection" /> used by this <see cref="T:System.Data.Common.DbCommand" />.
        /// </summary>
        public new SqlConnection Connection
        {
            get { return InnerSqlCommand.Connection; }
            set { InnerSqlCommand.Connection = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command object should be visible in a customized interface control.
        /// </summary>
        public override bool DesignTimeVisible
        {
            get { return InnerSqlCommand.DesignTimeVisible; }
            set { InnerSqlCommand.DesignTimeVisible = value; }
        }

        /// <summary>
        /// Gets or sets a value that specifies the System.Data.Sql.SqlNotificationRequest
        /// object bound to this command.
        /// </summary>
        public SqlNotificationRequest Notification
        {
            get { return InnerSqlCommand.Notification; }
            set { InnerSqlCommand.Notification = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application should automatically
        /// receive query notifications from a common System.Data.SqlClient.SqlDependency
        /// object.
        /// </summary>
        public bool NotificationAutoEnlist
        {
            get { return InnerSqlCommand.NotificationAutoEnlist; }
            set { InnerSqlCommand.NotificationAutoEnlist = value; }
        }

        /// <summary>
        /// Gets the collection of <see cref="T:System.Data.SqlClient.SqlParameter" /> objects.
        /// </summary>
        public new SqlParameterCollection Parameters
        {
            get { return InnerSqlCommand.Parameters; }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.SqlClient.SqlTransaction" /> within which this <see cref="T:System.Data.SqlClient.SqlCommand" /> object executes.
        /// </summary>
        public new SqlTransaction Transaction
        {
            get { return InnerSqlCommand.Transaction; }
            set { InnerSqlCommand.Transaction = value; }
        }

        /// <summary>
        /// Gets or sets how command results are applied to the <see cref="T:System.Data.DataRow" /> when used by the Update method of a <see cref="T:System.Data.Common.DbDataAdapter" />.
        /// </summary>
        public override UpdateRowSource UpdatedRowSource
        {
            get { return InnerSqlCommand.UpdatedRowSource; }
            set { InnerSqlCommand.UpdatedRowSource = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.Common.DbConnection" /> used by this <see cref="T:System.Data.Common.DbCommand" />.
        /// </summary>
        protected override DbConnection DbConnection
        {
            get { return Connection; }
            set { Connection = (SqlConnection)value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="P:System.Data.Common.DbCommand.DbTransaction" /> within which this <see cref="T:System.Data.Common.DbCommand" /> object executes.
        /// </summary>
        protected override DbTransaction DbTransaction
        {
            get { return Transaction; }
            set { Transaction = (SqlTransaction)value; }
        }

        /// <summary>
        /// Gets the collection of <see cref="T:System.Data.Common.DbParameter" /> objects.
        /// </summary>
        protected override DbParameterCollection DbParameterCollection
        {
            get { return Parameters; }
        }

        /// <summary>
        /// Begins the execute non query. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        public IAsyncResult BeginExecuteNonQuery()
        {
            return InnerSqlCommand.BeginExecuteNonQuery();
        }

        /// <summary>
        /// Begins the execute non query. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="stateObject">The state object.</param>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object", Justification = "Keep consistent with System.Data.SqlClient.SqlCommand")]
        public IAsyncResult BeginExecuteNonQuery(AsyncCallback callback, object stateObject)
        {
            return InnerSqlCommand.BeginExecuteNonQuery(callback, stateObject);
        }

        /// <summary>
        /// Begins the execute reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        public IAsyncResult BeginExecuteReader()
        {
            return InnerSqlCommand.BeginExecuteReader();
        }

        /// <summary>
        /// Begins the execute reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="behavior">The behavior.</param>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        public IAsyncResult BeginExecuteReader(CommandBehavior behavior)
        {
            return InnerSqlCommand.BeginExecuteReader(behavior);
        }

        /// <summary>
        /// Begins the execute reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="stateObject">The state object.</param>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object", Justification = "Keep consistent with System.Data.SqlClient.SqlCommand")]
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject)
        {
            return InnerSqlCommand.BeginExecuteReader(callback, stateObject);
        }

        /// <summary>
        /// Begins the execute reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="stateObject">The state object.</param>
        /// <param name="behavior">The behavior.</param>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object", Justification = "Keep consistent with System.Data.SqlClient.SqlCommand")]
        public IAsyncResult BeginExecuteReader(AsyncCallback callback, object stateObject, CommandBehavior behavior)
        {
            return InnerSqlCommand.BeginExecuteReader(callback, stateObject, behavior);
        }

        /// <summary>
        /// Begins the execute XML reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        public IAsyncResult BeginExecuteXmlReader()
        {
            return InnerSqlCommand.BeginExecuteXmlReader();
        }

        /// <summary>
        /// Begins the execute XML reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="stateObject">The state object.</param>
        /// <returns>An System.IAsyncResult that can be used to poll or wait for results, or both;</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object", Justification = "Keep consistent with System.Data.SqlClient.SqlCommand")]
        public IAsyncResult BeginExecuteXmlReader(AsyncCallback callback, object stateObject)
        {
            return InnerSqlCommand.BeginExecuteXmlReader(callback, stateObject);
        }

        /// <summary>
        /// Attempts to cancels the execution of a <see cref="T:System.Data.SqlClient.SqlCommand" />.
        /// </summary>
        public override void Cancel()
        {
            InnerSqlCommand.Cancel();
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A new <see cref="TraceableSqlCommand"/> object that is a copy of this instance.</returns>
        public TraceableSqlCommand Clone()
        {
            return new TraceableSqlCommand(this);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Creates the parameter. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>A System.Data.SqlClient.SqlParameter object.</returns>
        public new SqlParameter CreateParameter()
        {
            return InnerSqlCommand.CreateParameter();
        }

        /// <summary>
        /// Ends the execute non query. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        /// <returns>The number of rows affected (the same behavior as System.Data.SqlClient.SqlCommand.ExecuteNonQuery()).</returns>
        public int EndExecuteNonQuery(IAsyncResult asyncResult)
        {
            return InnerSqlCommand.EndExecuteNonQuery(asyncResult);
        }

        /// <summary>
        /// Ends the execute reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        /// <returns>A System.Data.SqlClient.SqlDataReader object that can be used to retrieve the requested rows.</returns>
        public SqlDataReader EndExecuteReader(IAsyncResult asyncResult)
        {
            return InnerSqlCommand.EndExecuteReader(asyncResult);
        }

        /// <summary>
        /// Ends the execute XML reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        /// <returns>An System.Xml.XmlReader object that can be used to fetch the resulting XML data.</returns>
        public XmlReader EndExecuteXmlReader(IAsyncResult asyncResult)
        {
            return InnerSqlCommand.EndExecuteXmlReader(asyncResult);
        }

        /// <summary>
        /// Executes a SQL statement against a connection object. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>
        /// The number of rows affected.
        /// </returns>
        public override int ExecuteNonQuery()
        {
            return Intercept(() => InnerSqlCommand.ExecuteNonQuery());
        }

        /// <summary>
        /// This is the asynchronous version of <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery" />. Providers should override with an appropriate implementation. 
        /// The cancellation token may optionally be ignored.The default implementation invokes the synchronous <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery" /> method and returns 
        /// a completed task, blocking the calling thread. The default implementation will return a cancelled task if passed an already cancelled cancellation token.  
        /// Exceptions thrown by <see cref="M:System.Data.Common.DbCommand.ExecuteNonQuery" /> will be communicated via the returned Task Exception property.Do not invoke other methods and properties 
        /// of the DbCommand object until the returned Task is complete.
        /// Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteNonQueryAsync(cancellationToken));
        }

        /// <summary>
        /// Executes the reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>A System.Data.SqlClient.SqlDataReader object.</returns>
        public new SqlDataReader ExecuteReader()
        {
            return Intercept(() => InnerSqlCommand.ExecuteReader());
        }

        /// <summary>
        /// Executes the reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="behavior">The behavior.</param>
        /// <returns>A System.Data.SqlClient.SqlDataReader object.</returns>
        public new SqlDataReader ExecuteReader(CommandBehavior behavior)
        {
            return Intercept(() => InnerSqlCommand.ExecuteReader(behavior));
        }

        /// <summary>
        /// Executes the reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public new Task<SqlDataReader> ExecuteReaderAsync()
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteReaderAsync());
        }

        /// <summary>
        /// Executes the reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public new Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteReaderAsync(cancellationToken));
        }

        /// <summary>
        /// Executes the reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="behavior">The behavior.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public new Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteReaderAsync(behavior));
        }

        /// <summary>
        /// Executes the reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="behavior">The behavior.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public new Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteReaderAsync(behavior, cancellationToken));
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set.
        /// </returns>
        public override object ExecuteScalar()
        {
            return Intercept(() => InnerSqlCommand.ExecuteScalar());
        }

        /// <summary>
        /// This is the asynchronous version of <see cref="M:System.Data.Common.DbCommand.ExecuteScalar" />. Providers should override with an appropriate implementation. 
        /// The cancellation token may optionally be ignored.The default implementation invokes the synchronous <see cref="M:System.Data.Common.DbCommand.ExecuteScalar" /> method and returns a 
        /// completed task, blocking the calling thread. The default implementation will return a cancelled task if passed an already cancelled cancellation token. Exceptions thrown by ExecuteScalar will be
        /// communicated via the returned Task Exception property.Do not invoke other methods and properties of the DbCommand object until the returned Task is complete.
        /// Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteScalarAsync(cancellationToken));
        }

        /// <summary>
        /// Executes the XML reader. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>An System.Xml.XmlReader object.</returns>
        public XmlReader ExecuteXmlReader()
        {
            return Intercept(() => InnerSqlCommand.ExecuteXmlReader());
        }

        /// <summary>
        /// Executes the XML reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<XmlReader> ExecuteXmlReaderAsync()
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteXmlReaderAsync());
        }

        /// <summary>
        /// Executes the XML reader asynchronous. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task<XmlReader> ExecuteXmlReaderAsync(CancellationToken cancellationToken)
        {
            return InterceptAsync(() => InnerSqlCommand.ExecuteXmlReaderAsync(cancellationToken));
        }

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        public override void Prepare()
        {
            InnerSqlCommand.Prepare();
        }

        /// <summary>
        /// Resets the command timeout. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        public void ResetCommandTimeout()
        {
            InnerSqlCommand.ResetCommandTimeout();
        }

        /// <summary>
        /// Creates a new instance of a <see cref="T:System.Data.Common.DbParameter" /> object. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbParameter" /> object.
        /// </returns>
        protected override DbParameter CreateDbParameter()
        {
            return InnerSqlCommand.CreateParameter();
        }

        /// <summary>
        /// Executes the command text against the connection. Wrapper of the same function in <see cref="T:System.Data.SqlClient.SqlCommand"/>.
        /// </summary>
        /// <param name="behavior">An instance of <see cref="T:System.Data.CommandBehavior" />.</param>
        /// <returns>
        /// A task representing the operation.
        /// </returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return Intercept(() => InnerSqlCommand.ExecuteReader(behavior));
        }

        private TResult Intercept<TResult>(Func<TResult> method)
            => _interceptor.Intercept(method, this);
        
        private async Task<TResult> InterceptAsync<TResult>(Func<Task<TResult>> method)
            => await _interceptor.InterceptAsync(method, this);
    }
}
