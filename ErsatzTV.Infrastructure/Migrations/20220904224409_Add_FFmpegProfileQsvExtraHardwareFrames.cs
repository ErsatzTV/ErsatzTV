using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_FFmpegProfileQsvExtraHardwareFrames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QsvExtraHardwareFrames",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: true,
                defaultValue: 64);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QsvExtraHardwareFrames",
                table: "FFmpegProfile");
        }
    }
}
