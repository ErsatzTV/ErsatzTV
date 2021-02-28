using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_SeasonMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "SeasonMetadataId",
                "Artwork",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                "SeasonMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    SeasonId = table.Column<int>("INTEGER", nullable: false),
                    MetadataKind = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    OriginalTitle = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    ReleaseDate = table.Column<DateTime>("TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>("TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_SeasonMetadata_Season_SeasonId",
                        x => x.SeasonId,
                        "Season",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Artwork_SeasonMetadataId",
                "Artwork",
                "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                "IX_SeasonMetadata_SeasonId",
                "SeasonMetadata",
                "SeasonId");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork",
                "SeasonMetadataId",
                "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork");

            migrationBuilder.DropTable(
                "SeasonMetadata");

            migrationBuilder.DropIndex(
                "IX_Artwork_SeasonMetadataId",
                "Artwork");

            migrationBuilder.DropColumn(
                "SeasonMetadataId",
                "Artwork");
        }
    }
}
