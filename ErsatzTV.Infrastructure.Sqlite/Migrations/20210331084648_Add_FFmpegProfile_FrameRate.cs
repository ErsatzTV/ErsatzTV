using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_FFmpegProfile_FrameRate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "NormalizeResolution",
                "FFmpegProfile");

            migrationBuilder.RenameColumn(
                "NormalizeVideoCodec",
                "FFmpegProfile",
                "NormalizeVideo");

            migrationBuilder.AddColumn<string>(
                "FrameRate",
                "FFmpegProfile",
                "TEXT",
                nullable: true,
                defaultValue: "24");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "FrameRate",
                "FFmpegProfile");

            migrationBuilder.RenameColumn(
                "NormalizeVideo",
                "FFmpegProfile",
                "NormalizeVideoCodec");

            migrationBuilder.AddColumn<bool>(
                "NormalizeResolution",
                "FFmpegProfile",
                "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
