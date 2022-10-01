using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_PlexLibraryScan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Id IN (SELECT Id FROM PlexLibrary)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
