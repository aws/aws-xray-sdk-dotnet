//-----------------------------------------------------------------------------
// <copyright file="EFUtil.cs" company="Amazon.com">
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
using System.Text.RegularExpressions;
using Amazon.XRay.Recorder.Core;
#if NET45
using Amazon.XRay.Recorder.Core.Internal.Utils;
#endif

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    /// <summary>
    /// Utilities for EFInterceptor
    /// </summary>
    public static class EFUtil
    {
        private static readonly string EntityFramework = "entityframework";
        private static readonly string SqlServerCompact35 = "sqlservercompact35";
        private static readonly string SqlServerCompact40 = "sqlservercompact40";
        private static readonly string MicrosoftSqlClient = "microsoft.data.sqlclient";
        private static readonly string SystemSqlClient = "system.data.sqlclient";
        private static readonly string SqlServer = "sqlserver";

        private static readonly string[] UserIdFormatOptions = { "user id", "username", "user", "userid" }; // case insensitive

        private static readonly string[] DatabaseTypes = { "sqlserver", "sqlite", "postgresql", "mysql", "firebirdsql",
                                                           "inmemory" , "cosmosdb" , "oracle" , "filecontextcore" ,
                                                           "jet" , "teradata" , "openedge" , "ibm" , "mycat" , "vfp"};

        private static readonly Regex _portNumberRegex = new Regex(@"[,|:]\d+$");

        private static readonly AWSXRayRecorder _recorder = AWSXRayRecorder.Instance;

        /// <summary>
        /// Process command to begin subsegment.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <param name="collectSqlQueriesOverride">Nullable to indicate whether to collect sql query text or not.</param>
        internal static void ProcessBeginCommand(DbCommand command, bool? collectSqlQueriesOverride)
        {
            _recorder.BeginSubsegment(BuildSubsegmentName(command));
            _recorder.SetNamespace("remote");
            CollectSqlInformation(command, collectSqlQueriesOverride);
        }

        /// <summary>
        /// Process to end subsegment
        /// </summary>
        internal static void ProcessEndCommand()
        {
            _recorder.EndSubsegment();
        }

        /// <summary>
        /// Process exception.
        /// </summary>
        /// <param name="exception">Instance of <see cref="Exception"/>.</param>
        internal static void ProcessCommandError(Exception exception)
        {
            _recorder.AddException(exception);
            _recorder.EndSubsegment();
        }

        /// <summary>
        /// Builds the name of the subsegment in the format database@datasource
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <returns>Returns the formed subsegment name as a string.</returns>
        private static string BuildSubsegmentName(DbCommand command)
            => command.Connection.Database + "@" + RemovePortNumberFromDataSource(command.Connection.DataSource);

        /// <summary>
        /// Records the SQL information on the current subsegment,
        /// </summary>
        private static void CollectSqlInformation(DbCommand command, bool? collectSqlQueriesOverride)
        {
            // Get database type from DbCommand
            string databaseType = GetDataBaseType(command);
            _recorder.AddSqlInformation("database_type", databaseType);

            _recorder.AddSqlInformation("database_version", command.Connection.ServerVersion);

            DbConnectionStringBuilder connectionStringBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = command.Connection.ConnectionString
            };

            // Remove sensitive information from connection string
            connectionStringBuilder.Remove("Password");

            // Do a pre-check for UserID since in the case of TrustedConnection, a UserID may not be available.
            var user_id = GetUserId(connectionStringBuilder);
            if (user_id != null)
            {
                _recorder.AddSqlInformation("user", user_id.ToString());
            }

            _recorder.AddSqlInformation("connection_string", connectionStringBuilder.ToString());

            if (ShouldCollectSqlText(collectSqlQueriesOverride))
            {
                _recorder.AddSqlInformation("sanitized_query", command.CommandText);
            }
        }

        /// <summary>
        /// Extract database_type from <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="command">Instance of <see cref="DbCommand"/>.</param>
        /// <returns>Type of database.</returns>
        internal static string GetDataBaseType(DbCommand command)
        {
            var typeString = command?.Connection?.GetType()?.FullName?.ToLower();

            if (string.IsNullOrEmpty(typeString))
            {
                return EntityFramework;
            }

            if (typeString.Contains(MicrosoftSqlClient) || typeString.Contains(SystemSqlClient))
            {
                return SqlServer;
            }

            if (typeString.Contains(SqlServerCompact35))
            {
                return SqlServerCompact35;
            }

            if (typeString.Contains(SqlServerCompact40))
            {
                return SqlServerCompact40;
            }

            foreach (var databaseType in DatabaseTypes)
            {
                if (typeString.Contains(databaseType))
                {
                    return databaseType;
                }
            }

            return typeString;
        }

        /// <summary>
        /// Extract user id from <see cref="DbConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="builder">Instance of <see cref="DbConnectionStringBuilder"/>.</param>
        /// <returns></returns>
        internal static object GetUserId(DbConnectionStringBuilder builder)
        {
            foreach (string key in UserIdFormatOptions)
            {
                if (builder.TryGetValue(key, out object value))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes the port number from data source.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <returns>The data source string with port number removed.</returns>
        private static string RemovePortNumberFromDataSource(string dataSource)
        {
            return _portNumberRegex.Replace(dataSource, string.Empty);
        }

#if !NET45
        private static bool ShouldCollectSqlText(bool? collectSqlQueriesOverride)
            => collectSqlQueriesOverride ?? _recorder.XRayOptions.CollectSqlQueries;
#else
        private static bool ShouldCollectSqlText(bool? collectSqlQueriesOverride)
            => collectSqlQueriesOverride ?? AppSettings.CollectSqlQueries;
#endif
    }
}
