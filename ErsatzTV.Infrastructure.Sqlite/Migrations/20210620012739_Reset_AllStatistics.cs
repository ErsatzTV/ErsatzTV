using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_AllStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE MediaVersion SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE LibraryFolder SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbySeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyEpisode SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinEpisode SET Etag = NULL");
            migrationBuilder.Sql("UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE Library SET LastScan = '0001-01-01 00:00:00'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
