﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MovieMetadataShowMetadataContentRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // local and plex
            migrationBuilder.Sql("UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE LibraryPathId IN
                    (SELECT LibraryPathId FROM LibraryPath LP
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    WHERE L.MediaKind = 1)");

            // emby
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");

            // jellyfin
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");

            migrationBuilder.AddColumn<string>(
                "ContentRating",
                "ShowMetadata",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "ContentRating",
                "MovieMetadata",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "ContentRating",
                "ShowMetadata");

            migrationBuilder.DropColumn(
                "ContentRating",
                "MovieMetadata");
        }
    }
}
