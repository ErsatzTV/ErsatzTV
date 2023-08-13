using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_EtagWithMissingPlexStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // clear etag on plex movies where no streams are present (caused by bug in recent version)
            migrationBuilder.Sql(
                @"UPDATE PlexMovie SET Etag = NULL WHERE PlexMovie.Id IN
                (SELECT PlexMovie.Id FROM PlexMovie
                INNER JOIN Movie M on M.Id = PlexMovie.Id
                INNER JOIN MediaVersion MV on M.Id = MV.MovieId
                LEFT JOIN MediaStream MS on MV.Id = MS.MediaVersionId
                WHERE MS.Id IS NULL)");

            // clear etag on plex episodes where no streams are present (caused by bug in recent version)
            migrationBuilder.Sql(
                @"UPDATE PlexEpisode SET Etag = NULL WHERE PlexEpisode.Id IN
                (SELECT PlexEpisode.Id FROM PlexEpisode
                INNER JOIN Episode E on E.Id = PlexEpisode.Id
                INNER JOIN MediaVersion MV on E.Id = MV.EpisodeId
                LEFT JOIN MediaStream MS on MV.Id = MS.MediaVersionId
                WHERE MS.Id IS NULL)");

            // force scanning libraries with NULL etag movies
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Library.Id IN
                (SELECT DISTINCT Library.Id FROM Library
                INNER JOIN LibraryPath LP on Library.Id = LP.LibraryId
                INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                INNER JOIN PlexMovie PM ON MI.Id = PM.Id
                WHERE PM.Etag IS NULL)");

            // force scanning libraries with NULL etag episodes
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Library.Id IN
                (SELECT DISTINCT Library.Id FROM Library
                INNER JOIN LibraryPath LP on Library.Id = LP.LibraryId
                INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                INNER JOIN PlexEpisode PE ON MI.Id = PE.Id
                WHERE PE.Etag IS NULL)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
