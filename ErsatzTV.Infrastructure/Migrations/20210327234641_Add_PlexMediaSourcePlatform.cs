using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlexMediaSourcePlatform : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "Platform",
                "PlexMediaSource",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "PlatformVersion",
                "PlexMediaSource",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Platform",
                "PlexMediaSource");

            migrationBuilder.DropColumn(
                "PlatformVersion",
                "PlexMediaSource");
        }
    }
}
