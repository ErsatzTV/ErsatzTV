using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MediaStreamPixelFormat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BitsPerRawSample",
                table: "MediaStream",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PixelFormat",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BitsPerRawSample",
                table: "MediaStream");

            migrationBuilder.DropColumn(
                name: "PixelFormat",
                table: "MediaStream");
        }
    }
}
