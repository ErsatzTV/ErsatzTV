using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_OtherVideo_OtherVideoMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Tag",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Studio",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "MetadataGuid",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoId",
                table: "MediaVersion",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Genre",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Artwork",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Actor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtherVideo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherVideo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherVideo_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtherVideoMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OtherVideoId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_OtherVideoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherVideoMetadata_OtherVideo_OtherVideoId",
                        column: x => x.OtherVideoId,
                        principalTable: "OtherVideo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tag_OtherVideoMetadataId",
                table: "Tag",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_OtherVideoMetadataId",
                table: "Studio",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_OtherVideoMetadataId",
                table: "MetadataGuid",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_OtherVideoId",
                table: "MediaVersion",
                column: "OtherVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_OtherVideoMetadataId",
                table: "Genre",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_OtherVideoMetadataId",
                table: "Artwork",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_OtherVideoMetadataId",
                table: "Actor",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherVideoMetadata_OtherVideoId",
                table: "OtherVideoMetadata",
                column: "OtherVideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Artwork_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Artwork",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaVersion_OtherVideo_OtherVideoId",
                table: "MediaVersion",
                column: "OtherVideoId",
                principalTable: "OtherVideo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Tag",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Artwork_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Artwork");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaVersion_OtherVideo_OtherVideoId",
                table: "MediaVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "OtherVideoMetadata");

            migrationBuilder.DropTable(
                name: "OtherVideo");

            migrationBuilder.DropIndex(
                name: "IX_Tag_OtherVideoMetadataId",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Studio_OtherVideoMetadataId",
                table: "Studio");

            migrationBuilder.DropIndex(
                name: "IX_MetadataGuid_OtherVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropIndex(
                name: "IX_MediaVersion_OtherVideoId",
                table: "MediaVersion");

            migrationBuilder.DropIndex(
                name: "IX_Genre_OtherVideoMetadataId",
                table: "Genre");

            migrationBuilder.DropIndex(
                name: "IX_Artwork_OtherVideoMetadataId",
                table: "Artwork");

            migrationBuilder.DropIndex(
                name: "IX_Actor_OtherVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Studio");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropColumn(
                name: "OtherVideoId",
                table: "MediaVersion");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Genre");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Actor");
        }
    }
}
