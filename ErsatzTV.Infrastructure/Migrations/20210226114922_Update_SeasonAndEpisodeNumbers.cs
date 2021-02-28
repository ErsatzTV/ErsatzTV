using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_SeasonAndEpisodeNumbers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Season SET SeasonNumber =
            (SELECT Number FROM TelevisionSeason ts INNER JOIN MediaItem mi on mi.Id = Season.Id AND mi.TelevisionSeasonId = ts.Id)
            WHERE SeasonNumber = 0");

            migrationBuilder.Sql(
                @"UPDATE Episode SET EpisodeNumber =
            (SELECT Episode FROM TelevisionEpisodeMetadata tem INNER JOIN MediaItem mi ON mi.Id = Episode.Id AND mi.TelevisionEpisodeId = tem.TelevisionEpisodeId)
            WHERE EpisodeNumber = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
