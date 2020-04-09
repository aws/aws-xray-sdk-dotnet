using System;
using System.Collections.Generic;
using System.Text;

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
