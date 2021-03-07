﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Peep.API.Persistence;

namespace Peep.API.Persistence.Migrations
{
    [DbContext(typeof(PeepApiContext))]
    [Migration("20210307010403_AddCompletedJobData")]
    partial class AddCompletedJobData
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Peep.API.Models.Entities.CompletedJob", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<int>("CompletionReason")
                        .HasColumnType("int");

                    b.Property<int>("CrawlCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateCompleted")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("DateQueued")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("DateStarted")
                        .HasColumnType("datetime(6)");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("time(6)");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("JobJson")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("CompletedJobs");
                });

            modelBuilder.Entity("Peep.API.Models.Entities.CompletedJobData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("CompletedJobId")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<string>("Source")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.Property<string>("Value")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.HasIndex("CompletedJobId");

                    b.ToTable("CompletedJobData");
                });

            modelBuilder.Entity("Peep.API.Models.Entities.QueuedJob", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<DateTime>("DateQueued")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("JobJson")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("QueuedJobs");
                });

            modelBuilder.Entity("Peep.API.Models.Entities.RunningJob", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(255) CHARACTER SET utf8mb4");

                    b.Property<int>("CrawlCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("DateCompleted")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("DateQueued")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("DateStarted")
                        .HasColumnType("datetime(6)");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("time(6)");

                    b.Property<string>("JobJson")
                        .HasColumnType("longtext CHARACTER SET utf8mb4");

                    b.HasKey("Id");

                    b.ToTable("RunningJobs");
                });

            modelBuilder.Entity("Peep.API.Models.Entities.CompletedJobData", b =>
                {
                    b.HasOne("Peep.API.Models.Entities.CompletedJob", "CompletedJob")
                        .WithMany("CompletedJobData")
                        .HasForeignKey("CompletedJobId");
                });
#pragma warning restore 612, 618
        }
    }
}
