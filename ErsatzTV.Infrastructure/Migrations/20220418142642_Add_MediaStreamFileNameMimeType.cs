using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MediaStreamFileNameMimeType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "MediaStream",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "MediaStream");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "MediaStream");
        }
    }
}
