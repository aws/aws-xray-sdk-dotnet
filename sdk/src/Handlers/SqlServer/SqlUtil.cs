//-----------------------------------------------------------------------------
// <copyright file="SqlUtil.cs" company="Amazon.com">
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

using System.Text.RegularExpressions;

namespace Amazon.XRay.Recorder.Handlers.SqlServer
{
    /// <summary>
    /// Utilities for SQL handlers
    /// </summary>
    public static class SqlUtil
    {
        // 1st alternative: (?:'([^']|'')*') matches single quoted literals, i.e. string, datatime.
        // Example:
        //      'apple'
        //      'very ''strong'''
        //      ''
        // 2nd alternative: (?:(-|\+)?\$?\d+(\.\d+)? matches number and money
        // Example:
        //      123.12
        //      -123
        //      +12
        //      $123.12
        //      -$123.12
        private static readonly Regex _sqlLiteralRegex = new Regex(@"(?:'([^']|'')*')|(?:(-|\+)?\$?\d+(\.\d+)?)");
        private static readonly Regex _portNumberRegex = new Regex(@",\d+$");

        /// <summary>
        /// Sanitizes the TSQL query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Sanitized query string</returns>
        public static string SanitizeTsqlQuery(string query)
        {
            return _sqlLiteralRegex.Replace(query, "?");
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
