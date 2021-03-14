using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Peep.API.Persistence.Migrations
{
    public partial class AddJobCrawler : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobCrawlers",
                columns: table => new
                {
                    CrawlerId = table.Column<string>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    LastHeartbeat = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCrawlers", x => x.CrawlerId);
                    table.ForeignKey(
                        name: "FK_JobCrawlers_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobCrawlers_JobId",
                table: "JobCrawlers",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobCrawlers");
        }
    }
}
