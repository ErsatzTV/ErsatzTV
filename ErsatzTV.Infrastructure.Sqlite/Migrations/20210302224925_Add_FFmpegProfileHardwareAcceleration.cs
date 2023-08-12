using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_FFmpegProfileHardwareAcceleration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<int>(
                "HardwareAcceleration",
                "FFmpegProfile",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "HardwareAcceleration",
                "FFmpegProfile");
    }
}
