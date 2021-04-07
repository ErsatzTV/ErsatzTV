using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_Artist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "ArtistMetadataId",
                "Tag",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "ArtistMetadataId",
                "Studio",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "ArtistId",
                "MusicVideo",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "ArtistMetadataId",
                "Genre",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "ArtistMetadataId",
                "Artwork",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                "Artist",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artist", x => x.Id);
                    table.ForeignKey(
                        "FK_Artist_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ArtistMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Disambiguation = table.Column<string>("TEXT", nullable: true),
                    Biography = table.Column<string>("TEXT", nullable: true),
                    Formed = table.Column<string>("TEXT", nullable: true),
                    ArtistId = table.Column<int>("INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_ArtistMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_ArtistMetadata_Artist_ArtistId",
                        x => x.ArtistId,
                        "Artist",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Mood",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    ArtistMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mood", x => x.Id);
                    table.ForeignKey(
                        "FK_Mood_ArtistMetadata_ArtistMetadataId",
                        x => x.ArtistMetadataId,
                        "ArtistMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Style",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    ArtistMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Style", x => x.Id);
                    table.ForeignKey(
                        "FK_Style_ArtistMetadata_ArtistMetadataId",
                        x => x.ArtistMetadataId,
                        "ArtistMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Tag_ArtistMetadataId",
                "Tag",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Studio_ArtistMetadataId",
                "Studio",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_MusicVideo_ArtistId",
                "MusicVideo",
                "ArtistId");

            migrationBuilder.CreateIndex(
                "IX_Genre_ArtistMetadataId",
                "Genre",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Artwork_ArtistMetadataId",
                "Artwork",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_ArtistMetadata_ArtistId",
                "ArtistMetadata",
                "ArtistId");

            migrationBuilder.CreateIndex(
                "IX_Mood_ArtistMetadataId",
                "Mood",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Style_ArtistMetadataId",
                "Style",
                "ArtistMetadataId");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_ArtistMetadata_ArtistMetadataId",
                "Artwork",
                "ArtistMetadataId",
                "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Genre_ArtistMetadata_ArtistMetadataId",
                "Genre",
                "ArtistMetadataId",
                "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_MusicVideo_Artist_ArtistId",
                "MusicVideo",
                "ArtistId",
                "Artist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Studio_ArtistMetadata_ArtistMetadataId",
                "Studio",
                "ArtistMetadataId",
                "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Tag_ArtistMetadata_ArtistMetadataId",
                "Tag",
                "ArtistMetadataId",
                "ArtistMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_ArtistMetadata_ArtistMetadataId",
                "Artwork");

            migrationBuilder.DropForeignKey(
                "FK_Genre_ArtistMetadata_ArtistMetadataId",
                "Genre");

            migrationBuilder.DropForeignKey(
                "FK_MusicVideo_Artist_ArtistId",
                "MusicVideo");

            migrationBuilder.DropForeignKey(
                "FK_Studio_ArtistMetadata_ArtistMetadataId",
                "Studio");

            migrationBuilder.DropForeignKey(
                "FK_Tag_ArtistMetadata_ArtistMetadataId",
                "Tag");

            migrationBuilder.DropTable(
                "Mood");

            migrationBuilder.DropTable(
                "Style");

            migrationBuilder.DropTable(
                "ArtistMetadata");

            migrationBuilder.DropTable(
                "Artist");

            migrationBuilder.DropIndex(
                "IX_Tag_ArtistMetadataId",
                "Tag");

            migrationBuilder.DropIndex(
                "IX_Studio_ArtistMetadataId",
                "Studio");

            migrationBuilder.DropIndex(
                "IX_MusicVideo_ArtistId",
                "MusicVideo");

            migrationBuilder.DropIndex(
                "IX_Genre_ArtistMetadataId",
                "Genre");

            migrationBuilder.DropIndex(
                "IX_Artwork_ArtistMetadataId",
                "Artwork");

            migrationBuilder.DropColumn(
                "ArtistMetadataId",
                "Tag");

            migrationBuilder.DropColumn(
                "ArtistMetadataId",
                "Studio");

            migrationBuilder.DropColumn(
                "ArtistId",
                "MusicVideo");

            migrationBuilder.DropColumn(
                "ArtistMetadataId",
                "Genre");

            migrationBuilder.DropColumn(
                "ArtistMetadataId",
                "Artwork");
        }
    }
}
