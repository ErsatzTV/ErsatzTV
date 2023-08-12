using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_JellyfinMediaSourceOperatingSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<string>(
                "OperatingSystem",
                "JellyfinMediaSource",
                "TEXT",
                nullable: true);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "OperatingSystem",
                "JellyfinMediaSource");
    }
}
