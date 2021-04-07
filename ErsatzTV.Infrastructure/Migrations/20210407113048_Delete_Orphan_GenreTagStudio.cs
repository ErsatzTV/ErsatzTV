using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Delete_Orphan_GenreTagStudio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM Genre
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM Show)
                OR SeasonMetadataId NOT IN (SELECT Id FROM Season)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM Episode)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");

            migrationBuilder.Sql(
                @"DELETE FROM Tag
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM Show)
                OR SeasonMetadataId NOT IN (SELECT Id FROM Season)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM Episode)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");

            migrationBuilder.Sql(
                @"DELETE FROM Studio
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM Show)
                OR SeasonMetadataId NOT IN (SELECT Id FROM Season)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM Episode)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
