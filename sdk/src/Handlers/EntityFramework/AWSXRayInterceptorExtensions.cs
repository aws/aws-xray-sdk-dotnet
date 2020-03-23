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

using Microsoft.EntityFrameworkCore;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    /// <summary>
    /// Extension method for <see cref="DbContextOptionsBuilder"/> to add <see cref="EFInterceptor"/>.
    /// User can pass collectSqlQueries to AddXRayInterceptor() to decide if sanitized_query should be included in the trace
    /// context or not.
    /// </summary>
    public static class AWSXRayInterceptorExtensions
    {
        /// <summary>
        /// Add <see cref="EFInterceptor"/> to <see cref="DbContextOptionsBuilder"/>.
        /// </summary>
        /// <param name="dbContextOptionsBuilder">Instance of <see cref="DbContextOptionsBuilder"/>.</param>
        /// <param name="collectSqlQueries"></param>
        /// <returns>Instance of <see cref="DbContextOptionsBuilder"/>.</returns>
        public static DbContextOptionsBuilder AddXRayInterceptor(this DbContextOptionsBuilder dbContextOptionsBuilder, bool? collectSqlQueries = null)
        {
            return dbContextOptionsBuilder.AddInterceptors(new EFInterceptor(collectSqlQueries));
        }
    }
}
