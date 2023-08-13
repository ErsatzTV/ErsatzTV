using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_LocalSeasonEtag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL
                WHERE LibraryPathId IN
                (SELECT MI.LibraryPathId FROM MediaItem MI
                INNER JOIN Season S on MI.Id = S.Id
                INNER JOIN LibraryPath LP on MI.LibraryPathId = LP.Id
                INNER JOIN Library L on LP.LibraryId = L.Id
                INNER JOIN LocalLibrary LL on L.Id = LL.Id
                WHERE L.MediaKind = 2)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
