using Microsoft.EntityFrameworkCore;
using Peep.API.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Persistence
{
    public class PeepApiContext : DbContext
    {
        public PeepApiContext(DbContextOptions<PeepApiContext> options) : base(options)
        {

        }

        public DbSet<QueuedJob> QueuedJobs { get; set; }
        public DbSet<RunningJob> RunningJobs { get; set; }
        public DbSet<CompletedJob> CompletedJobs { get; set; }
        public DbSet<ErroredJob> ErroredJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueuedJob>().ToTable("QueuedJobs");
            modelBuilder.Entity<RunningJob>().ToTable("RunningJobs");
            modelBuilder.Entity<CompletedJob>().ToTable("CompletedJobs");
            modelBuilder.Entity<ErroredJob>().ToTable("ErroredJobs");
        }
    }
}
