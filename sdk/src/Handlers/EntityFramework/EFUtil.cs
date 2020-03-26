//-----------------------------------------------------------------------------
// <copyright file="EFUtil.cs" company="Amazon.com">
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

using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    /// <summary>
    /// Utilities for EFInterceptor
    /// </summary>
    public static class EFUtil
    {
        private static readonly string DefaultDatabaseType = "EntityFrameworkCore";

        private static readonly string[] UserIdFormatOptions = { "user id", "username", "user", "userid" }; // case insensitive

        // Some database providers may not support Entity Framework Core 3.0 and above now
        // https://docs.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli
        private static readonly Dictionary<string, string> DatabaseTypes = new Dictionary<string, string>()
        {
            // Support tracing
            { "Microsoft.EntityFrameworkCore.SqlServer" , "sqlserver" },
            { "Microsoft.EntityFrameworkCore.Sqlite" , "sqlite" },
            { "Npgsql.EntityFrameworkCore.PostgreSQL" , "postgresql" },
            { "Pomelo.EntityFrameworkCore.MySql" , "mysql" },
            { "FirebirdSql.EntityFrameworkCore.Firebird" , "firebirdsql" },

            // Not support tracing so far
            { "Microsoft.EntityFrameworkCore.InMemory" , "inmemory" },
            { "Microsoft.EntityFrameworkCore.Cosmos" , "cosmosdb" },
            { "Devart.Data.MySql.EFCore" , "mysql" },
            { "Devart.Data.Oracle.EFCore" , "oracle" },
            { "Devart.Data.PostgreSql.EFCore" , "postgresql" },
            { "Devart.Data.SQLite.EFCore" , "sqlite" },
            { "FileContextCore" , "filecontextcore" },
            { "EntityFrameworkCore.Jet" , "jet" },
            { "EntityFrameworkCore.SqlServerCompact35" , "sqlservercompact35" },
            { "EntityFrameworkCore.SqlServerCompact40" , "sqlservercompact40" },
            { "Teradata.EntityFrameworkCore" , "teradata" },
            { "EntityFrameworkCore.FirebirdSql" , "firebirdsql" },
            { "EntityFrameworkCore.OpenEdge" , "openedge" },
            { "MySql.Data.EntityFrameworkCore" , "mysql" },
            { "Oracle.EntityFrameworkCore" , "oracle" },
            { "IBM.EntityFrameworkCore" , "ibm" },
            { "IBM.EntityFrameworkCore-lnx" , "ibm" },
            { "IBM.EntityFrameworkCore-osx" , "ibm" },
            { "Pomelo.EntityFrameworkCore.MyCat" , "mycat" }
        };

        private static readonly Regex _portNumberRegex = new Regex(@"[,|:]\d+$");

        /// <summary>
        /// Extract database_type from <see cref="DbContext"/>.
        /// </summary>
        /// <param name="context">Instance of <see cref="DbContext"/>.</param>
        /// <returns>Type of database.</returns>
        public static string GetDataBaseType(DbContext context)
        {
            string databaseProvider = context?.Database?.ProviderName;

            // Need to check if the context and its following parameter is null or not to avoid exception
            if (string.IsNullOrEmpty(databaseProvider))
            {
                return DefaultDatabaseType;
            }

            string value = null;

            if (DatabaseTypes.TryGetValue(databaseProvider, out value))
            {
                return value;
            }

            return databaseProvider;
        }

        /// <summary>
        /// Extract user id from <see cref="DbConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="builder">Instance of <see cref="DbConnectionStringBuilder"/>.</param>
        /// <returns></returns>
        public static object GetConnectionValue(DbConnectionStringBuilder builder)
        {
            object value = null;
            foreach (string key in UserIdFormatOptions)
            {
                if (builder.TryGetValue(key, out value))
                {
                    break;
                }
            }
            return value;
        }

        /// <summary>
        /// Removes the port number from data source.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <returns>The data source string with port number removed.</returns>
        public static string RemovePortNumberFromDataSource(string dataSource)
        {
            return _portNumberRegex.Replace(dataSource, string.Empty);
        }
    }
}
