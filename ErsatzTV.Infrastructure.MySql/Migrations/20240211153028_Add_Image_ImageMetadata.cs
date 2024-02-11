using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_Image_ImageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Tag",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Subtitle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Studio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "MetadataGuid",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "MediaVersion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Genre",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Artwork",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageMetadataId",
                table: "Actor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Image",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Image", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Image_MediaItem_Id",
                        column: x => x.Id,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImageMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ImageId = table.Column<int>(type: "int", nullable: false),
                    MetadataKind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortTitle = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageMetadata_Image_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Image",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_ImageMetadataId",
                table: "Tag",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_ImageMetadataId",
                table: "Subtitle",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Studio_ImageMetadataId",
                table: "Studio",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_ImageMetadataId",
                table: "MetadataGuid",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaVersion_ImageId",
                table: "MediaVersion",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Genre_ImageMetadataId",
                table: "Genre",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Artwork_ImageMetadataId",
                table: "Artwork",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ImageMetadataId",
                table: "Actor",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ImageMetadata_ImageId",
                table: "ImageMetadata",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_ImageMetadata_ImageMetadataId",
                table: "Actor",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Artwork_ImageMetadata_ImageMetadataId",
                table: "Artwork",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_ImageMetadata_ImageMetadataId",
                table: "Genre",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaVersion_Image_ImageId",
                table: "MediaVersion",
                column: "ImageId",
                principalTable: "Image",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_ImageMetadata_ImageMetadataId",
                table: "MetadataGuid",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_ImageMetadata_ImageMetadataId",
                table: "Studio",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subtitle_ImageMetadata_ImageMetadataId",
                table: "Subtitle",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_ImageMetadata_ImageMetadataId",
                table: "Tag",
                column: "ImageMetadataId",
                principalTable: "ImageMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_ImageMetadata_ImageMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Artwork_ImageMetadata_ImageMetadataId",
                table: "Artwork");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_ImageMetadata_ImageMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaVersion_Image_ImageId",
                table: "MediaVersion");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_ImageMetadata_ImageMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_ImageMetadata_ImageMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtitle_ImageMetadata_ImageMetadataId",
                table: "Subtitle");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_ImageMetadata_ImageMetadataId",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "ImageMetadata");

            migrationBuilder.DropTable(
                name: "Image");

            migrationBuilder.DropIndex(
                name: "IX_Tag_ImageMetadataId",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Subtitle_ImageMetadataId",
                table: "Subtitle");

            migrationBuilder.DropIndex(
                name: "IX_Studio_ImageMetadataId",
                table: "Studio");

            migrationBuilder.DropIndex(
                name: "IX_MetadataGuid_ImageMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropIndex(
                name: "IX_MediaVersion_ImageId",
                table: "MediaVersion");

            migrationBuilder.DropIndex(
                name: "IX_Genre_ImageMetadataId",
                table: "Genre");

            migrationBuilder.DropIndex(
                name: "IX_Artwork_ImageMetadataId",
                table: "Artwork");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ImageMetadataId",
                table: "Actor");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Studio");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "MediaVersion");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Genre");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Actor");
        }
    }
}
