using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class RebuildAllPlayoutsUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PlayoutItem");
            migrationBuilder.Sql(@"DELETE FROM PlayoutProgramScheduleAnchor");
            migrationBuilder.Sql(@"UPDATE Playout SET Anchor_NextStart = null, Anchor_NextScheduleItemId = null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
