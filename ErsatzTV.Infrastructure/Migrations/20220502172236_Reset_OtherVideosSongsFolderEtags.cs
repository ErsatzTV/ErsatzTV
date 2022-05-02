using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_OtherVideosSongsFolderEtags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM LibraryFolder WHERE Id IN
(SELECT LF.Id FROM LibraryFolder LF
INNER JOIN LibraryPath LP on LP.Id = LF.LibraryPathId
INNER JOIN Library L on L.Id = LP.LibraryId
WHERE L.MediaKind IN (4, 5))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
