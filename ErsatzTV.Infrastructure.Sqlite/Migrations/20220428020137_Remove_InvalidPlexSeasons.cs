using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Remove_InvalidPlexSeasons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PlexSeason WHERE Key LIKE '%allLeaves%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
