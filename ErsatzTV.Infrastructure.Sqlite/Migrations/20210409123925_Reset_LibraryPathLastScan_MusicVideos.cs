using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_LibraryPathLastScan_MusicVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (SELECT LP.Id FROM LibraryPath LP INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 3)");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
