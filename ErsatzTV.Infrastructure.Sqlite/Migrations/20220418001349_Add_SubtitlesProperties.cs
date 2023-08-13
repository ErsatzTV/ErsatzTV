using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_SubtitlesProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codec",
                table: "Subtitle",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Default",
                table: "Subtitle",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Forced",
                table: "Subtitle",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Subtitle",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StreamIndex",
                table: "Subtitle",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codec",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "Default",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "Forced",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Subtitle");

            migrationBuilder.DropColumn(
                name: "StreamIndex",
                table: "Subtitle");
        }
    }
}
