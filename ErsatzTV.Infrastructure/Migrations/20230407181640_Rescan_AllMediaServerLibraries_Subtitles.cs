using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Rescan_AllMediaServerLibraries_Subtitles : Migration
    {
        /// <inheritdoc />
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

            migrationBuilder.Sql("UPDATE PlexMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE PlexShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE PlexSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE PlexEpisode SET Etag = NULL");
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Id IN (SELECT Id FROM PlexLibrary)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
