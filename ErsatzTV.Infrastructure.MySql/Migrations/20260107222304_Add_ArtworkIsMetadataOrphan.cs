using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_ArtworkIsMetadataOrphan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMetadataOrphan",
                table: "Artwork",
                type: "tinyint(1)",
                nullable: true,
                computedColumnSql: "CASE WHEN COALESCE(ArtistMetadataId, ChannelId, EpisodeMetadataId, MovieMetadataId, MusicVideoMetadataId, OtherVideoMetadataId, SeasonMetadataId, ShowMetadataId, SongMetadataId, ImageMetadataId, RemoteStreamMetadataId) IS NULL THEN 1 ELSE NULL END",
                stored: false);

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_IsMetadataOrphan",
                table: "Artwork",
                column: "IsMetadataOrphan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Artwork_IsMetadataOrphan",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "IsMetadataOrphan",
                table: "Artwork");
        }
    }
}
