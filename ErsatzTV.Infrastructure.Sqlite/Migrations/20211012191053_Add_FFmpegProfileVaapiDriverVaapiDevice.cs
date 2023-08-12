using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_FFmpegProfileVaapiDriverVaapiDevice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VaapiDevice",
                table: "FFmpegProfile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VaapiDriver",
                table: "FFmpegProfile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VaapiDevice",
                table: "FFmpegProfile");

            migrationBuilder.DropColumn(
                name: "VaapiDriver",
                table: "FFmpegProfile");
        }
    }
}
