using Microsoft.EntityFrameworkCore.Migrations;

namespace Peep.API.Persistence.Migrations
{
    public partial class JobStateAsString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "State",
                table: "Jobs",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "State",
                table: "Jobs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
