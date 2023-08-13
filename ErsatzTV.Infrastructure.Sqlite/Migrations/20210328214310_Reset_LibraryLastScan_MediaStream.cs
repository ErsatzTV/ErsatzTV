using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_LibraryLastScan_MediaStream : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(@"UPDATE Library SET LastScan = '0001-01-01 00:00:00'");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
