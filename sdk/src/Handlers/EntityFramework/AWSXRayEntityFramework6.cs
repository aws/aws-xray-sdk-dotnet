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
    /// Class to add EFInterceptor to DbInterception to enable tracing Sql queries through EF 6.
    /// </summary>
    public static class AWSXRayEntityFramework6
    {
        /// <summary>
        /// Add <see cref="EFInterceptor"/> to <see cref="DbInterception"/>.
        /// </summary>
        /// <param name="collectSqlQueries">Nullable to indicate whether to record sql query text or not. Default value is null.</param>
        public static void AddXRayInterceptor(bool? collectSqlQueries = null)
        {
            DbInterception.Add(new EFInterceptor(collectSqlQueries));
        }
    }
}
