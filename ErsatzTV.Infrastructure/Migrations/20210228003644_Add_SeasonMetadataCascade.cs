using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_SeasonMetadataCascade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork",
                "SeasonMetadataId",
                "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_SeasonMetadata_SeasonMetadataId",
                "Artwork",
                "SeasonMetadataId",
                "SeasonMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
