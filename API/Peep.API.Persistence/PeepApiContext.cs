using Microsoft.EntityFrameworkCore;
using Peep.API.Models.Entities;
using System;
using Peep.API.Models.Enums;
using Peep.Core.Infrastructure;

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
        public DbSet<JobCrawler> JobCrawlers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // convert to string into db
            // convert to JobState out of db
            modelBuilder
                .Entity<Job>()
                .Property(p => p.State)
                .HasConversion(
                    v => v.ToString(),
                    v => (JobState) Enum.Parse(typeof(JobState), v));

            // convert to string into db
            // convert to CrawlerId out of db
            modelBuilder
                .Entity<JobCrawler>()
                .Property(p => p.CrawlerId)
                .HasConversion(
                    v => v.Value,
                    v => new CrawlerId(v));

            modelBuilder
                .Entity<JobCrawler>()
                .HasKey(jc => jc.CrawlerId);
            
            modelBuilder
                .Entity<Job>()
                .ToTable("Jobs");
            
            modelBuilder.Entity<JobData>().ToTable("JobData");
            modelBuilder.Entity<JobError>().ToTable("JobErrors");
            modelBuilder.Entity<JobCrawler>().ToTable("JobCrawlers");
        }
    }
}
