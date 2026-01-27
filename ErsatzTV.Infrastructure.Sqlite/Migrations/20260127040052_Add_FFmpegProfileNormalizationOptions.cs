using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_FFmpegProfileNormalizationOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NormalizeAudio",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NormalizeColors",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NormalizeVideo",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizeAudio",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "NormalizeColors",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "NormalizeVideo",
                table: "FFmpegProfile");
        }
    }
}
