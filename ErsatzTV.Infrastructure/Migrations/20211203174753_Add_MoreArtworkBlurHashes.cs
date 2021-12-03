using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MoreArtworkBlurHashes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlurHash",
                table: "Artwork",
                newName: "BlurHash64");

            migrationBuilder.AddColumn<string>(
                name: "BlurHash43",
                table: "Artwork",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlurHash54",
                table: "Artwork",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlurHash43",
                table: "Artwork");

            migrationBuilder.DropColumn(
                name: "BlurHash54",
                table: "Artwork");

            migrationBuilder.RenameColumn(
                name: "BlurHash64",
                table: "Artwork",
                newName: "BlurHash");
        }
    }
}
