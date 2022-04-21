using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlexEtags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Etag",
                table: "PlexShow",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etag",
                table: "PlexSeason",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etag",
                table: "PlexMovie",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Etag",
                table: "PlexEpisode",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Etag",
                table: "PlexShow");

            migrationBuilder.DropColumn(
                name: "Etag",
                table: "PlexSeason");

            migrationBuilder.DropColumn(
                name: "Etag",
                table: "PlexMovie");

            migrationBuilder.DropColumn(
                name: "Etag",
                table: "PlexEpisode");
        }
    }
}
