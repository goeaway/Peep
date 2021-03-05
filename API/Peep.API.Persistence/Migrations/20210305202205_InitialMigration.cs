using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Peep.API.Persistence.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletedJobs",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    JobJson = table.Column<string>(nullable: true),
                    DataJson = table.Column<string>(nullable: true),
                    DateQueued = table.Column<DateTime>(nullable: false),
                    DateStarted = table.Column<DateTime>(nullable: false),
                    DateCompleted = table.Column<DateTime>(nullable: false),
                    CrawlCount = table.Column<int>(nullable: false),
                    Duration = table.Column<TimeSpan>(nullable: false),
                    CompletionReason = table.Column<int>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueuedJobs",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    JobJson = table.Column<string>(nullable: true),
                    DateQueued = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RunningJobs",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    JobJson = table.Column<string>(nullable: true),
                    DateQueued = table.Column<DateTime>(nullable: false),
                    DateStarted = table.Column<DateTime>(nullable: false),
                    DateCompleted = table.Column<DateTime>(nullable: true),
                    CrawlCount = table.Column<int>(nullable: false),
                    Duration = table.Column<TimeSpan>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningJobs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedJobs");

            migrationBuilder.DropTable(
                name: "QueuedJobs");

            migrationBuilder.DropTable(
                name: "RunningJobs");
        }
    }
}
