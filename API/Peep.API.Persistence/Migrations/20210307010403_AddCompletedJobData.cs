using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Peep.API.Persistence.Migrations
{
    public partial class AddCompletedJobData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "CompletedJobs");

            migrationBuilder.CreateTable(
                name: "CompletedJobData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Source = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                    CompletedJobId = table.Column<string>(nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletedJobData");

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "CompletedJobs",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);
        }
    }
}
