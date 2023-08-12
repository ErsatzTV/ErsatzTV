using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Remove_FFmpegProfile_NormalizeAudioCodec : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "NormalizeAudioCodec",
                "FFmpegProfile");

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<bool>(
                "NormalizeAudioCodec",
                "FFmpegProfile",
                "INTEGER",
                nullable: false,
                defaultValue: false);
    }
}
