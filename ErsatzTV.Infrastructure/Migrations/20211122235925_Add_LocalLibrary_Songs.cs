using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_LocalLibrary_Songs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create local songs library
            migrationBuilder.Sql(
                @"INSERT INTO Library (Name, MediaKind, MediaSourceId)
                SELECT 'Songs', 5, Id FROM
                (SELECT LMS.Id FROM LocalMediaSource LMS LIMIT 1)");
            migrationBuilder.Sql("INSERT INTO LocalLibrary (Id) Values (last_insert_rowid())");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
