using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MusicVideo_MusicVideoMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "MusicVideoMetadataId",
                "Tag",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MusicVideoMetadataId",
                "Studio",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MusicVideoId",
                "MediaVersion",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MusicVideoMetadataId",
                "Genre",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MusicVideoMetadataId",
                "Artwork",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                "MusicVideo",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideo", x => x.Id);
                    table.ForeignKey(
                        "FK_MusicVideo_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "MusicVideoMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Album = table.Column<string>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Artist = table.Column<string>("TEXT", nullable: true),
                    MusicVideoId = table.Column<int>("INTEGER", nullable: false),
                    MetadataKind = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    OriginalTitle = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    Year = table.Column<int>("INTEGER", nullable: true),
                    ReleaseDate = table.Column<DateTime>("TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>("TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideoMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_MusicVideoMetadata_MusicVideo_MusicVideoId",
                        x => x.MusicVideoId,
                        "MusicVideo",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Tag_MusicVideoMetadataId",
                "Tag",
                "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Studio_MusicVideoMetadataId",
                "Studio",
                "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                "IX_MediaVersion_MusicVideoId",
                "MediaVersion",
                "MusicVideoId");

            migrationBuilder.CreateIndex(
                "IX_Genre_MusicVideoMetadataId",
                "Genre",
                "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Artwork_MusicVideoMetadataId",
                "Artwork",
                "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                "IX_MusicVideoMetadata_MusicVideoId",
                "MusicVideoMetadata",
                "MusicVideoId");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_MusicVideoMetadata_MusicVideoMetadataId",
                "Artwork",
                "MusicVideoMetadataId",
                "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Genre_MusicVideoMetadata_MusicVideoMetadataId",
                "Genre",
                "MusicVideoMetadataId",
                "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MediaVersion_MusicVideo_MusicVideoId",
                "MediaVersion",
                "MusicVideoId",
                "MusicVideo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Studio_MusicVideoMetadata_MusicVideoMetadataId",
                "Studio",
                "MusicVideoMetadataId",
                "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Tag_MusicVideoMetadata_MusicVideoMetadataId",
                "Tag",
                "MusicVideoMetadataId",
                "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_MusicVideoMetadata_MusicVideoMetadataId",
                "Artwork");

            migrationBuilder.DropForeignKey(
                "FK_Genre_MusicVideoMetadata_MusicVideoMetadataId",
                "Genre");

            migrationBuilder.DropForeignKey(
                "FK_MediaVersion_MusicVideo_MusicVideoId",
                "MediaVersion");

            migrationBuilder.DropForeignKey(
                "FK_Studio_MusicVideoMetadata_MusicVideoMetadataId",
                "Studio");

            migrationBuilder.DropForeignKey(
                "FK_Tag_MusicVideoMetadata_MusicVideoMetadataId",
                "Tag");

            migrationBuilder.DropTable(
                "MusicVideoMetadata");

            migrationBuilder.DropTable(
                "MusicVideo");

            migrationBuilder.DropIndex(
                "IX_Tag_MusicVideoMetadataId",
                "Tag");

            migrationBuilder.DropIndex(
                "IX_Studio_MusicVideoMetadataId",
                "Studio");

            migrationBuilder.DropIndex(
                "IX_MediaVersion_MusicVideoId",
                "MediaVersion");

            migrationBuilder.DropIndex(
                "IX_Genre_MusicVideoMetadataId",
                "Genre");

            migrationBuilder.DropIndex(
                "IX_Artwork_MusicVideoMetadataId",
                "Artwork");

            migrationBuilder.DropColumn(
                "MusicVideoMetadataId",
                "Tag");

            migrationBuilder.DropColumn(
                "MusicVideoMetadataId",
                "Studio");

            migrationBuilder.DropColumn(
                "MusicVideoId",
                "MediaVersion");

            migrationBuilder.DropColumn(
                "MusicVideoMetadataId",
                "Genre");

            migrationBuilder.DropColumn(
                "MusicVideoMetadataId",
                "Artwork");
        }
    }
}
