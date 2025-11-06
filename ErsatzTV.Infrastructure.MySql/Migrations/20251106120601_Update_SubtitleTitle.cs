using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Update_SubtitleTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Subtitle s
INNER JOIN EpisodeMetadata em ON em.Id = s.EpisodeMetadataId
INNER JOIN MediaVersion mv ON mv.EpisodeId = em.EpisodeId
INNER JOIN MediaStream ms ON ms.MediaVersionId = mv.Id AND ms.MediaStreamKind = 3 AND ms.`Index` = s.StreamIndex
SET s.Title = ms.Title
WHERE s.Title != ms.Title;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
