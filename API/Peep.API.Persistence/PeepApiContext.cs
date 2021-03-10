using Microsoft.EntityFrameworkCore;
using Peep.API.Models.Entities;
using System;
using Peep.API.Models.Enums;

namespace Peep.API.Persistence
{
    public class PeepApiContext : DbContext
    {
        public PeepApiContext(DbContextOptions<PeepApiContext> options) : base(options)
        {

        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobData> JobData { get; set; }
        public DbSet<JobError> JobErrors { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Job>()
                .Property(p => p.State)
                .HasConversion(
                    v => v.ToString(),
                    v => (JobState) Enum.Parse(typeof(JobState), v));
            
            modelBuilder
                .Entity<Job>()
                .ToTable("Jobs");
            
            modelBuilder.Entity<JobData>().ToTable("JobData");
            modelBuilder.Entity<JobError>().ToTable("JobErrors");
        }
    }
}
