using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_FFmpegProfile_NormalizeLoudness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "AudioVolume",
                "FFmpegProfile");

            migrationBuilder.AddColumn<bool>(
                "NormalizeLoudness",
                "FFmpegProfile",
                "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "NormalizeLoudness",
                "FFmpegProfile");

            migrationBuilder.AddColumn<int>(
                "AudioVolume",
                "FFmpegProfile",
                "INTEGER",
                nullable: false);
        }
    }
}
