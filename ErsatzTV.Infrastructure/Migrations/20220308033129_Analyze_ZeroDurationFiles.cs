using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Analyze_ZeroDurationFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE Id IN
                (
                    SELECT LF.Id FROM LibraryFolder LF
                    INNER JOIN LibraryPath LP on LF.LibraryPathId = LP.Id
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                    INNER JOIN MediaVersion MV on MI.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                    WHERE MV.Duration = '00:00:00.0000000'
                )");

            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (
                    SELECT LP.Id FROM LibraryPath LP
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                    INNER JOIN MediaVersion MV on MI.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                    WHERE MV.Duration = '00:00:00.0000000'
                )");

            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (
                    SELECT L.Id FROM Library L
                    INNER JOIN LibraryPath LP on L.Id = LP.LibraryId
                    INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                    INNER JOIN MediaVersion MV on MI.Id = COALESCE(MovieId, MusicVideoId, OtherVideoId, SongId, EpisodeId)
                    WHERE MV.Duration = '00:00:00.0000000'
                )");

            migrationBuilder.Sql(
                @"UPDATE MediaVersion SET DateUpdated = '0001-01-01 00:00:00' WHERE Duration = '00:00:00.0000000'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
