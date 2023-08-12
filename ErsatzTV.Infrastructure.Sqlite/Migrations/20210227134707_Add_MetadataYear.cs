using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MetadataYear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "Year",
                "ShowMetadata",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Year",
                "SeasonMetadata",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Year",
                "MovieMetadata",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Year",
                "EpisodeMetadata",
                "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Year",
                "ShowMetadata");

            migrationBuilder.DropColumn(
                "Year",
                "SeasonMetadata");

            migrationBuilder.DropColumn(
                "Year",
                "MovieMetadata");

            migrationBuilder.DropColumn(
                "Year",
                "EpisodeMetadata");
        }
    }
}
