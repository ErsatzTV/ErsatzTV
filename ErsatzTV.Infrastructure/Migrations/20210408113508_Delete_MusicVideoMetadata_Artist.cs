using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Delete_MusicVideoMetadata_Artist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "Artist",
                "MusicVideoMetadata");

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<string>(
                "Artist",
                "MusicVideoMetadata",
                "TEXT",
                nullable: true);
    }
}
