using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Delete_Orphan_GenreTagStudio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM Genre
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM ShowMetadata)
                OR SeasonMetadataId NOT IN (SELECT Id FROM SeasonMetadata)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM EpisodeMetadata)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");

            migrationBuilder.Sql(
                @"DELETE FROM Tag
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM ShowMetadata)
                OR SeasonMetadataId NOT IN (SELECT Id FROM SeasonMetadata)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM EpisodeMetadata)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");

            migrationBuilder.Sql(
                @"DELETE FROM Studio
                WHERE MovieMetadataId NOT IN (SELECT Id FROM MovieMetadata)
                OR ShowMetadataId NOT IN (SELECT Id FROM ShowMetadata)
                OR SeasonMetadataId NOT IN (SELECT Id FROM SeasonMetadata)
                OR EpisodeMetadataId NOT IN (SELECT Id FROM EpisodeMetadata)
                OR MusicVideoMetadataId NOT IN (SELECT Id FROM MusicVideoMetadata)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
