using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_FFmpegProfileNormalizeLoudnessMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NormalizeLoudnessMode",
                table: "FFmpegProfile",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("update FFmpegProfile set NormalizeLoudnessMode = 1 where NormalizeLoudness = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizeLoudnessMode",
                table: "FFmpegProfile");
        }
    }
}
