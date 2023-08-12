using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Remove_FFmpegProfileTranscodeNormalizeVideoNormalizeAudio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizeAudio",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "NormalizeVideo",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "Transcode",
                table: "FFmpegProfile");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NormalizeAudio",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NormalizeVideo",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Transcode",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
