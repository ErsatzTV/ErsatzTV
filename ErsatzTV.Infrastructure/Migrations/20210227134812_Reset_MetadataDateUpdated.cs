using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_MetadataDateUpdated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(@"UPDATE ShowMetadata SET Year = substr(ReleaseDate, 1, 4)");
            migrationBuilder.Sql(@"UPDATE SeasonMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(@"UPDATE EpisodeMetadata SET DateUpdated = '0001-01-01 00:00:00'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
