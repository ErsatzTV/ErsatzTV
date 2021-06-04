using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_EpisodeMetadataEpisodeNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE EpisodeMetadata SET EpisodeNumber = (SELECT EpisodeNumber FROM Episode WHERE Id = EpisodeMetadata.EpisodeId)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
