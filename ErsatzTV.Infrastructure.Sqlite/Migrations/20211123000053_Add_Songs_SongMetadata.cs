using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_Songs_SongMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "Tag",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "Studio",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "MetadataGuid",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongId",
                table: "MediaVersion",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "Genre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "Artwork",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SongMetadataId",
                table: "Actor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Song",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Song", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Song_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SongMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataKind = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalTitle = table.Column<string>(type: "TEXT", nullable: true),
                    SortTitle = table.Column<string>(type: "TEXT", nullable: true),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SongMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SongMetadata_Song_SongId",
                        column: x => x.SongId,
                        principalTable: "Song",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tag_SongMetadataId",
                table: "Tag",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_SongMetadataId",
                table: "Studio",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_SongMetadataId",
                table: "MetadataGuid",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_SongId",
                table: "MediaVersion",
                column: "SongId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_SongMetadataId",
                table: "Genre",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_SongMetadataId",
                table: "Artwork",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_SongMetadataId",
                table: "Actor",
                column: "SongMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_SongMetadata_SongId",
                table: "SongMetadata",
                column: "SongId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Artwork_SongMetadata_SongMetadataId",
                table: "Artwork",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaVersion_Song_SongId",
                table: "MediaVersion",
                column: "SongId",
                principalTable: "Song",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_SongMetadata_SongMetadataId",
                table: "Tag",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_SongMetadata_SongMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Artwork_SongMetadata_SongMetadataId",
                table: "Artwork");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaVersion_Song_SongId",
                table: "MediaVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_SongMetadata_SongMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_SongMetadata_SongMetadataId",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "SongMetadata");

            migrationBuilder.DropTable(
                name: "Song");

            migrationBuilder.DropIndex(
                name: "IX_Tag_SongMetadataId",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Studio_SongMetadataId",
                table: "Studio");

            migrationBuilder.DropIndex(
                name: "IX_MetadataGuid_SongMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropIndex(
                name: "IX_MediaVersion_SongId",
                table: "MediaVersion");

            migrationBuilder.DropIndex(
                name: "IX_Genre_SongMetadataId",
                table: "Genre");

            migrationBuilder.DropIndex(
                name: "IX_Artwork_SongMetadataId",
                table: "Artwork");

            migrationBuilder.DropIndex(
                name: "IX_Actor_SongMetadataId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "Studio");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropColumn(
                name: "SongId",
                table: "MediaVersion");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "Genre");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "SongMetadataId",
                table: "Actor");
        }
    }
}
