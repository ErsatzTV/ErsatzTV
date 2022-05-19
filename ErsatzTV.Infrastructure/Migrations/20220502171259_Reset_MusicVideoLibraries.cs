using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_MusicVideoLibraries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (SELECT LP.Id FROM LibraryPath LP INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 3)");

            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE MediaKind = 3");

            migrationBuilder.Sql(
                @"UPDATE MusicVideoMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            
            migrationBuilder.Sql(
                @"DELETE FROM LibraryFolder WHERE Id IN
(SELECT LF.Id FROM LibraryFolder LF
INNER JOIN LibraryPath LP on LP.Id = LF.LibraryPathId
INNER JOIN Library L on L.Id = LP.LibraryId
WHERE L.MediaKind = 3)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
