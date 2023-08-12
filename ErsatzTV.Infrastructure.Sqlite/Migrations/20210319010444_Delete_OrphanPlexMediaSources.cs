using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Delete_OrphanPlexMediaSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                @"DELETE FROM MediaSource WHERE Id NOT IN
                (SELECT Id FROM LocalMediaSource UNION ALL SELECT Id FROM PlexMediaSource)");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
