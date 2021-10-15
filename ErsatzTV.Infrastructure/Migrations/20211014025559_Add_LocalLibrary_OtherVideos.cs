using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_LocalLibrary_OtherVideos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create local other videos library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Other Videos', 4, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS LIMIT 1)");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
