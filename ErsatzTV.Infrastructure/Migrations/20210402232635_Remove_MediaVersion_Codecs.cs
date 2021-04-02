using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_MediaVersion_Codecs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "AudioCodec",
                "MediaVersion");

            migrationBuilder.DropColumn(
                "VideoCodec",
                "MediaVersion");

            migrationBuilder.DropColumn(
                "VideoProfile",
                "MediaVersion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "AudioCodec",
                "MediaVersion",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "VideoCodec",
                "MediaVersion",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "VideoProfile",
                "MediaVersion",
                "TEXT",
                nullable: true);
        }
    }
}
