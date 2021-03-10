using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Peep.API.Persistence.Migrations
{
    public partial class SimplifiedStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedJobData");

            migrationBuilder.DropTable(
                name: "QueuedJobs");

            migrationBuilder.DropTable(
                name: "RunningJobs");

            migrationBuilder.DropTable(
                name: "CompletedJobs");

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    JobJson = table.Column<string>(nullable: true),
                    DateQueued = table.Column<DateTime>(nullable: false),
                    DateStarted = table.Column<DateTime>(nullable: true),
                    DateCompleted = table.Column<DateTime>(nullable: true),
                    CrawlCount = table.Column<int>(nullable: false),
                    State = table.Column<int>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Source = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    JobId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobData_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobData_JobId",
                table: "JobData",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobData");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.CreateTable(
                name: "CompletedJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CompletionReason = table.Column<int>(type: "integer", nullable: false),
                    CrawlCount = table.Column<int>(type: "integer", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateQueued = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateStarted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    JobJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueuedJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DateQueued = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    JobJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RunningJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CrawlCount = table.Column<int>(type: "integer", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DateQueued = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DateStarted = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    JobJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompletedJobData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompletedJobId = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedJobData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedJobData_CompletedJobs_CompletedJobId",
                        column: x => x.CompletedJobId,
                        principalTable: "CompletedJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletedJobData_CompletedJobId",
                table: "CompletedJobData",
                column: "CompletedJobId");
        }
    }
}
