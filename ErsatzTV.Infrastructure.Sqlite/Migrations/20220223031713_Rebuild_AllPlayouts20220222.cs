using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Rebuild_AllPlayouts20220222 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PlayoutItem");
            migrationBuilder.Sql(@"DELETE FROM PlayoutProgramScheduleAnchor");
            migrationBuilder.Sql(@"DELETE FROM PlayoutAnchor");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
