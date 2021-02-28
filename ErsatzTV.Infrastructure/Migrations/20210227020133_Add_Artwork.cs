using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_Artwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Poster",
                "MediaItem");

            migrationBuilder.DropColumn(
                "PosterLastWriteTime",
                "MediaItem");

            migrationBuilder.CreateTable(
                "Artwork",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>("TEXT", nullable: true),
                    ArtworkKind = table.Column<int>("INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>("TEXT", nullable: false),
                    EpisodeMetadataId = table.Column<int>("INTEGER", nullable: true),
                    MovieMetadataId = table.Column<int>("INTEGER", nullable: true),
                    ShowMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artwork", x => x.Id);
                    table.ForeignKey(
                        "FK_Artwork_EpisodeMetadata_EpisodeMetadataId",
                        x => x.EpisodeMetadataId,
                        "EpisodeMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Artwork_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Artwork_ShowMetadata_ShowMetadataId",
                        x => x.ShowMetadataId,
                        "ShowMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Artwork_EpisodeMetadataId",
                "Artwork",
                "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Artwork_MovieMetadataId",
                "Artwork",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Artwork_ShowMetadataId",
                "Artwork",
                "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Artwork");

            migrationBuilder.AddColumn<string>(
                "Poster",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "PosterLastWriteTime",
                "MediaItem",
                "TEXT",
                nullable: true);
        }
    }
}
