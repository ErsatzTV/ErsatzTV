using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ArtistMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

            migrationBuilder.Sql(
                @"INSERT INTO MediaItem (LibraryPathId)
                SELECT LibraryPath.Id FROM LibraryPath INNER JOIN Library L on LibraryPath.LibraryId = L.Id
                WHERE MediaKind = 3");

            migrationBuilder.Sql(
                @"INSERT INTO Artist (Id)
                SELECT MediaItem.Id FROM MediaItem
                INNER JOIN LibraryPath LP on MediaItem.LibraryPathId = LP.Id
                INNER JOIN Library L on LP.LibraryId = L.Id
                WHERE MediaKind = 3 AND NOT EXISTS (SELECT Id FROM MusicVideo WHERE Id = MediaItem.Id)");

            migrationBuilder.Sql(
                $@"INSERT INTO ArtistMetadata (ArtistId, Title, DateAdded, DateUpdated, MetadataKind)
                SELECT Id, '[FAKE ARTIST]', '{now}', '{now}', 0 FROM Artist");

            migrationBuilder.Sql(
                @"UPDATE MusicVideo SET ArtistId =
                (SELECT Artist.Id FROM Artist
                INNER JOIN MediaItem MIA on Artist.Id = MIA.Id
                INNER JOIN MediaItem MIMV on MusicVideo.Id = MIMV.Id
                INNER JOIN LibraryPath LPA on MIA.LibraryPathId = LPA.Id
                INNER JOIN LibraryPath LPMV on MIMV.LibraryPathId = LPMV.Id
                WHERE LPA.LibraryId = LPMV.LibraryId)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
