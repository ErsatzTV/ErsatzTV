using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_EmbyMovieShowEtagForTagItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // clear etag on all emby movies
            migrationBuilder.Sql(@"UPDATE EmbyMovie SET Etag = NULL");

            // clear etag on all emby shows
            migrationBuilder.Sql(@"UPDATE EmbyShow SET Etag = NULL");

            // force scanning libraries with NULL etag movies
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Library.Id IN
                (SELECT DISTINCT Library.Id FROM Library
                INNER JOIN LibraryPath LP on Library.Id = LP.LibraryId
                INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                INNER JOIN EmbyMovie EM ON MI.Id = EM.Id
                WHERE EM.Etag IS NULL)");

            // force scanning libraries with NULL etag shows
            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE Library.Id IN
                (SELECT DISTINCT Library.Id FROM Library
                INNER JOIN LibraryPath LP on Library.Id = LP.LibraryId
                INNER JOIN MediaItem MI on LP.Id = MI.LibraryPathId
                INNER JOIN EmbyShow ES ON MI.Id = ES.Id
                WHERE ES.Etag IS NULL)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
