using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MediaStreamColor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorPrimaries",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorRange",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorSpace",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorTransfer",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorPrimaries",
                table: "MediaStream");

            migrationBuilder.DropColumn(
                name: "ColorRange",
                table: "MediaStream");

            migrationBuilder.DropColumn(
                name: "ColorSpace",
                table: "MediaStream");

            migrationBuilder.DropColumn(
                name: "ColorTransfer",
                table: "MediaStream");
        }
    }
}
