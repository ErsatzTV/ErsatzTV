using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Rescan_EmbyJellyfinLibrariesPathInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbySeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyEpisode SET Etag = NULL");
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Id IN (SELECT Id FROM EmbyLibrary)");

            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinEpisode SET Etag = NULL");
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Id IN (SELECT Id FROM JellyfinLibrary)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
