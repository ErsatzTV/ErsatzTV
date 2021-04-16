using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_Actor_Artwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "ArtworkId",
                "Actor",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                "IX_Actor_ArtworkId",
                "Actor",
                "ArtworkId",
                unique: true);

            migrationBuilder.AddForeignKey(
                "FK_Actor_Artwork_ArtworkId",
                "Actor",
                "ArtworkId",
                "Artwork",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Actor_Artwork_ArtworkId",
                "Actor");

            migrationBuilder.DropIndex(
                "IX_Actor_ArtworkId",
                "Actor");

            migrationBuilder.DropColumn(
                "ArtworkId",
                "Actor");
        }
    }
}
