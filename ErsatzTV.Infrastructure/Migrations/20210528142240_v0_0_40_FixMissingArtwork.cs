using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class v0_0_40_FixMissingArtwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // local and plex
            migrationBuilder.Sql("UPDATE Artwork SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE SeasonMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE EpisodeMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE LibraryPathId IN
                    (SELECT LibraryPathId FROM LibraryPath LP
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    WHERE L.MediaKind = 1)");

            // emby
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbySeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyEpisode SET Etag = NULL");

            // jellyfin
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinEpisode SET Etag = NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}