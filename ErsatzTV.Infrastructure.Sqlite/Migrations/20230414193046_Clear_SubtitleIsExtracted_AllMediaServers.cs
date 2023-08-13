using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Clear_SubtitleIsExtracted_AllMediaServers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
update Subtitle set IsExtracted = 0 where Id in
  (select Subtitle.Id
         from Subtitle
                  inner join EpisodeMetadata EM on Subtitle.EpisodeMetadataId = EM.Id
                  inner join MediaItem MI on EM.EpisodeId = MI.Id
                  inner join LibraryPath LP on MI.LibraryPathId = LP.Id
                  inner join PlexLibrary PL on PL.Id = LP.LibraryId
         where Subtitle.Codec = 'srt'
           and IsExtracted = 1)");

            migrationBuilder.Sql(
                @"
update Subtitle set IsExtracted = 0 where Id in
  (select Subtitle.Id
         from Subtitle
                  inner join EpisodeMetadata EM on Subtitle.EpisodeMetadataId = EM.Id
                  inner join MediaItem MI on EM.EpisodeId = MI.Id
                  inner join LibraryPath LP on MI.LibraryPathId = LP.Id
                  inner join EmbyLibrary EL on EL.Id = LP.LibraryId
         where Subtitle.Codec = 'srt'
           and IsExtracted = 1)");

            migrationBuilder.Sql(
                @"
update Subtitle set IsExtracted = 0 where Id in
  (select Subtitle.Id
         from Subtitle
                  inner join EpisodeMetadata EM on Subtitle.EpisodeMetadataId = EM.Id
                  inner join MediaItem MI on EM.EpisodeId = MI.Id
                  inner join LibraryPath LP on MI.LibraryPathId = LP.Id
                  inner join JellyfinLibrary JL on JL.Id = LP.LibraryId
         where Subtitle.Codec = 'srt'
           and IsExtracted = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
