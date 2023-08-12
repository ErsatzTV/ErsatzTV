using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ChannelArtwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "ChannelId",
                "Artwork",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                "IX_Artwork_ChannelId",
                "Artwork",
                "ChannelId");

            migrationBuilder.AddForeignKey(
                "FK_Artwork_Channel_ChannelId",
                "Artwork",
                "ChannelId",
                "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Artwork_Channel_ChannelId",
                "Artwork");

            migrationBuilder.DropIndex(
                "IX_Artwork_ChannelId",
                "Artwork");

            migrationBuilder.DropColumn(
                "ChannelId",
                "Artwork");
        }
    }
}
