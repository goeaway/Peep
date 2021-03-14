using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Peep.API.Persistence.Migrations
{
    public partial class AddLastHeartbeat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeat",
                table: "Jobs",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHeartbeat",
                table: "Jobs");
        }
    }
}
