using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Update_SubtitleTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"WITH Match AS (
                    SELECT s.Id AS SubtitleId, ms.Title AS NewTitle
                    FROM Subtitle AS s
                    JOIN EpisodeMetadata AS em ON em.Id = s.EpisodeMetadataId
                    JOIN MediaVersion AS mv ON mv.EpisodeId = em.EpisodeId
                    JOIN MediaStream AS ms ON ms.MediaVersionId = mv.Id
                    WHERE ms.MediaStreamKind = 3
                      AND ms.`Index` = s.StreamIndex
                      AND s.Title != ms.Title
                )
                UPDATE Subtitle
                SET Title = (SELECT NewTitle FROM Match WHERE Match.SubtitleId = Subtitle.Id)
                WHERE Id IN (SELECT SubtitleId FROM Match);
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
