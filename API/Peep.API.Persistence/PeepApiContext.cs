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

        /// <summary>
        /// Gets or sets processed payments
        /// </summary>
        public DbSet<QueuedJob> QueuedJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<QueuedJob>().ToTable("QueuedJobs");
        }
    }
}
