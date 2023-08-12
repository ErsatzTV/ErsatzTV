using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class LibraryRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create local media source to attach all paths to
            migrationBuilder.Sql("INSERT INTO MediaSources (SourceType) Values (99)");
            migrationBuilder.Sql("INSERT INTO LocalMediaSources (Id, MediaType) Values (last_insert_rowid(), 99)");

            migrationBuilder.CreateTable(
                "Library",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    MediaKind = table.Column<int>("INTEGER", nullable: false),
                    LastScan = table.Column<DateTimeOffset>("TEXT", nullable: true),
                    MediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Library", x => x.Id);
                    table.ForeignKey(
                        "FK_Library_MediaSources_MediaSourceId",
                        x => x.MediaSourceId,
                        "MediaSources",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateTable(
                "LocalLibrary",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalLibrary", x => x.Id);
                    table.ForeignKey(
                        "FK_LocalLibrary_Library_Id",
                        x => x.Id,
                        "Library",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // create local movies library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
            SELECT 'Movies', 1, Id FROM LocalMediaSources WHERE MediaType = 99");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");

            // create local shows library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
            SELECT 'Shows', 2, Id FROM LocalMediaSources WHERE MediaType = 99");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");

            migrationBuilder.CreateTable(
                "LibraryPath",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>("TEXT", nullable: true),
                    LibraryId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryPath", x => x.Id);
                    table.ForeignKey(
                        "FK_LibraryPath_Library_LibraryId",
                        x => x.LibraryId,
                        "Library",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // migrate movie source/folders to library paths
            migrationBuilder.Sql(
                @"INSERT INTO LibraryPath (Path, LibraryId)
            SELECT lms.Folder, l.Id
            FROM LocalMediaSources lms
            LEFT OUTER JOIN Library l ON l.MediaKind = 1
            WHERE lms.MediaType = 2");

            // migrate show source/folders to library paths
            migrationBuilder.Sql(
                @"INSERT INTO LibraryPath (Path, LibraryId)
            SELECT lms.Folder, l.Id
            FROM LocalMediaSources lms
            LEFT OUTER JOIN Library l ON l.MediaKind = 2
            WHERE lms.MediaType = 1");

            // migrate media item links
            migrationBuilder.AddColumn<int>(
                "LibraryPathId",
                "MediaItems",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE MediaItems SET LibraryPathId =
                (SELECT l.Id FROM LibraryPath l INNER JOIN LocalMediaSources lms ON lms.Folder = l.Path WHERE lms.Id = MediaItems.MediaSourceId)");

            migrationBuilder.DropColumn(
                "MediaSourceId",
                "MediaItems");

            migrationBuilder.DropIndex(
                "IX_MediaItems_MediaSourceId",
                "MediaItems");

            migrationBuilder.DropForeignKey(
                "FK_MediaItems_MediaSources_MediaSourceId",
                "MediaItems");

            migrationBuilder.CreateIndex(
                "IX_MediaItems_LibraryPathId",
                "MediaItems",
                "LibraryPathId");

            migrationBuilder.DropForeignKey(
                "FK_LocalMediaSources_MediaSources_Id",
                "LocalMediaSources");

            migrationBuilder.DropForeignKey(
                "FK_PlexMediaSources_MediaSources_Id",
                "PlexMediaSources");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowSource_LocalMediaSources_MediaSourceId",
                "TelevisionShowSource");

            migrationBuilder.DropTable(
                "PlexMediaSourceConnections");

            migrationBuilder.DropTable(
                "PlexMediaSourceLibraries");

            migrationBuilder.DropIndex(
                "IX_MediaSources_Name",
                "MediaSources");

            migrationBuilder.DropPrimaryKey(
                "PK_PlexMediaSources",
                "PlexMediaSources");

            migrationBuilder.DropPrimaryKey(
                "PK_LocalMediaSources",
                "LocalMediaSources");

            migrationBuilder.DropColumn(
                "LastScan",
                "MediaSources");

            migrationBuilder.DropColumn(
                "Name",
                "MediaSources");

            migrationBuilder.DropColumn(
                "SourceType",
                "MediaSources");

            migrationBuilder.DropColumn(
                "Folder",
                "LocalMediaSources");

            migrationBuilder.DropColumn(
                "MediaType",
                "LocalMediaSources");

            migrationBuilder.RenameTable(
                "PlexMediaSources",
                newName: "PlexMediaSource");

            migrationBuilder.RenameTable(
                "LocalMediaSources",
                newName: "LocalMediaSource");

            migrationBuilder.AddColumn<int>(
                "MovieId1",
                "MovieMetadata",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "ServerName",
                "PlexMediaSource",
                "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                "PK_PlexMediaSource",
                "PlexMediaSource",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_LocalMediaSource",
                "LocalMediaSource",
                "Id");

            migrationBuilder.CreateTable(
                "PlexConnection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>("INTEGER", nullable: false),
                    Uri = table.Column<string>("TEXT", nullable: true),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexConnection", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexConnection_PlexMediaSource_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlexMediaItemPart",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlexId = table.Column<int>("INTEGER", nullable: false),
                    Key = table.Column<string>("TEXT", nullable: true),
                    Duration = table.Column<int>("INTEGER", nullable: false),
                    File = table.Column<string>("TEXT", nullable: true),
                    Size = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_PlexMediaItemPart", x => x.Id); });

            migrationBuilder.CreateTable(
                "PlexLibrary",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    ShouldSyncItems = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexLibrary", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexLibrary_Library_Id",
                        x => x.Id,
                        "Library",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "PlexMovies",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    PartId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMovies", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMovies_Movies_Id",
                        x => x.Id,
                        "Movies",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_PlexMovies_PlexMediaItemPart_PartId",
                        x => x.PartId,
                        "PlexMediaItemPart",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_MovieMetadata_MovieId1",
                "MovieMetadata",
                "MovieId1");

            migrationBuilder.CreateIndex(
                "IX_Library_MediaSourceId",
                "Library",
                "MediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_LibraryPath_LibraryId",
                "LibraryPath",
                "LibraryId");

            migrationBuilder.CreateIndex(
                "IX_PlexConnection_PlexMediaSourceId",
                "PlexConnection",
                "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_PlexMovies_PartId",
                "PlexMovies",
                "PartId");

            migrationBuilder.AddForeignKey(
                "FK_LocalMediaSource_MediaSources_Id",
                "LocalMediaSource",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MediaItems_LibraryPath_LibraryPathId",
                "MediaItems",
                "LibraryPathId",
                "LibraryPath",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MovieMetadata_Movies_MovieId1",
                "MovieMetadata",
                "MovieId1",
                "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_PlexMediaSource_MediaSources_Id",
                "PlexMediaSource",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowSource_LocalMediaSource_MediaSourceId",
                "TelevisionShowSource",
                "MediaSourceId",
                "LocalMediaSource",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_LocalMediaSource_MediaSources_Id",
                "LocalMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_MediaItems_LibraryPath_LibraryPathId",
                "MediaItems");

            migrationBuilder.DropForeignKey(
                "FK_MovieMetadata_Movies_MovieId1",
                "MovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_PlexMediaSource_MediaSources_Id",
                "PlexMediaSource");

            migrationBuilder.DropForeignKey(
                "FK_TelevisionShowSource_LocalMediaSource_MediaSourceId",
                "TelevisionShowSource");

            migrationBuilder.DropTable(
                "LibraryPath");

            migrationBuilder.DropTable(
                "LocalLibrary");

            migrationBuilder.DropTable(
                "PlexConnection");

            migrationBuilder.DropTable(
                "PlexLibrary");

            migrationBuilder.DropTable(
                "PlexMovies");

            migrationBuilder.DropTable(
                "Library");

            migrationBuilder.DropTable(
                "PlexMediaItemPart");

            migrationBuilder.DropIndex(
                "IX_MovieMetadata_MovieId1",
                "MovieMetadata");

            migrationBuilder.DropPrimaryKey(
                "PK_PlexMediaSource",
                "PlexMediaSource");

            migrationBuilder.DropPrimaryKey(
                "PK_LocalMediaSource",
                "LocalMediaSource");

            migrationBuilder.DropColumn(
                "MovieId1",
                "MovieMetadata");

            migrationBuilder.DropColumn(
                "ServerName",
                "PlexMediaSource");

            migrationBuilder.RenameTable(
                "PlexMediaSource",
                newName: "PlexMediaSources");

            migrationBuilder.RenameTable(
                "LocalMediaSource",
                newName: "LocalMediaSources");

            migrationBuilder.RenameColumn(
                "LibraryPathId",
                "MediaItems",
                "MediaSourceId");

            migrationBuilder.RenameIndex(
                "IX_MediaItems_LibraryPathId",
                table: "MediaItems",
                newName: "IX_MediaItems_MediaSourceId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                "LastScan",
                "MediaSources",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Name",
                "MediaSources",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "SourceType",
                "MediaSources",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                "Folder",
                "LocalMediaSources",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MediaType",
                "LocalMediaSources",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                "PK_PlexMediaSources",
                "PlexMediaSources",
                "Id");

            migrationBuilder.AddPrimaryKey(
                "PK_LocalMediaSources",
                "LocalMediaSources",
                "Id");

            migrationBuilder.CreateTable(
                "PlexMediaSourceConnections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>("INTEGER", nullable: false),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: true),
                    Uri = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSourceConnections", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaSourceConnections_PlexMediaSources_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSources",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "PlexMediaSourceLibraries",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>("TEXT", nullable: true),
                    MediaType = table.Column<int>("INTEGER", nullable: false),
                    Name = table.Column<string>("TEXT", nullable: true),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexMediaSourceLibraries", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexMediaSourceLibraries_PlexMediaSources_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSources",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_MediaSources_Name",
                "MediaSources",
                "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_PlexMediaSourceConnections_PlexMediaSourceId",
                "PlexMediaSourceConnections",
                "PlexMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_PlexMediaSourceLibraries_PlexMediaSourceId",
                "PlexMediaSourceLibraries",
                "PlexMediaSourceId");

            migrationBuilder.AddForeignKey(
                "FK_LocalMediaSources_MediaSources_Id",
                "LocalMediaSources",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MediaItems_MediaSources_MediaSourceId",
                "MediaItems",
                "MediaSourceId",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlexMediaSources_MediaSources_Id",
                "PlexMediaSources",
                "Id",
                "MediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_TelevisionShowSource_LocalMediaSources_MediaSourceId",
                "TelevisionShowSource",
                "MediaSourceId",
                "LocalMediaSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
