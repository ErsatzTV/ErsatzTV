using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class RemoveOldMediaSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            // remove old source/folders
            migrationBuilder.Sql(@"DELETE FROM MediaSources WHERE Id NOT IN (SELECT MediaSourceId FROM Library)");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
