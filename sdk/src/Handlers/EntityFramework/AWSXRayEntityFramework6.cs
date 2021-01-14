//-----------------------------------------------------------------------------
// <copyright file="AWSXRayEntityFramework6.cs" company="Amazon.com">
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

using System.Data.Entity.Infrastructure.Interception;

namespace Amazon.XRay.Recorder.Handlers.EntityFramework
{
    /// <summary>
    /// Class for <see cref="DbInterception"/> to add <see cref="EFInterceptor"/>.
    /// User can pass collectSqlQueries to AddXRayInterceptor() to decide if sanitized_query should be included in the trace
    /// context or not.
    /// </summary>
    public static class AWSXRayEntityFramework6
    {
        /// <summary>
        /// Enable tracing SQL queries through EntityFramework 6 for .NET framework by calling AWSXRayEntityFramework6.AddXRayInterceptor() to add <see cref="EFInterceptor"/> into <see cref="DbInterception"/> to register X-Ray tracing interceptor.
        /// </summary>
        /// <param name="collectSqlQueries">Set this parameter to true to capture sql query text. The value set here overrides the value of CollectSqlQueries in Web.config if present. The default value of this parameter is null.</param>
        public static void AddXRayInterceptor(bool? collectSqlQueries = null)
        {
            DbInterception.Add(new EFInterceptor(collectSqlQueries));
        }
    }
}
