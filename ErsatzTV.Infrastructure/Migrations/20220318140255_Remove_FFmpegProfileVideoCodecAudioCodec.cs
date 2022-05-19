using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_FFmpegProfileVideoCodecAudioCodec : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioCodec",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "VideoCodec",
                table: "FFmpegProfile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioCodec",
                table: "FFmpegProfile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoCodec",
                table: "FFmpegProfile",
                type: "TEXT",
                nullable: true);
        }
    }
}
