﻿//-----------------------------------------------------------------------------
// <copyright file="TestEFContext.cs" company="Amazon.com">
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

using Microsoft.EntityFrameworkCore;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public class TestEFContext : DbContext
    {
        public TestEFContext(DbContextOptions options) : base(options) { }

        public TestEFContext(DbContextOptions<TestEFContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

    }

    public class User
    {
        public int UserId { get; set; }
    }
}
