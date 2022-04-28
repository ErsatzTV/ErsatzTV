using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_FFmpegProfileSubtitleMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubtitleMode",
                table: "FFmpegProfile");

            migrationBuilder.RenameColumn(
                name: "PreferredLanguageCode",
                table: "Channel",
                newName: "PreferredAudioLanguageCode");

            migrationBuilder.AddColumn<string>(
                name: "PreferredSubtitleLanguageCode",
                table: "Channel",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubtitleMode",
                table: "Channel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredSubtitleLanguageCode",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "SubtitleMode",
                table: "Channel");

            migrationBuilder.RenameColumn(
                name: "PreferredAudioLanguageCode",
                table: "Channel",
                newName: "PreferredLanguageCode");

            migrationBuilder.AddColumn<int>(
                name: "SubtitleMode",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
