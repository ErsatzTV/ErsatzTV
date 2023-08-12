using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MediaItemPathIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // delete orphan movies, if any exist
            migrationBuilder.Sql(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT mi.Id FROM Library l
                INNER JOIN LibraryPath lp ON l.Id = lp.LibraryId
                INNER JOIN MediaItem mi ON lp.Id = mi.LibraryPathId
                LEFT OUTER JOIN Movie m ON mi.Id = m.Id
                WHERE l.MediaKind = 1 AND m.Id IS NULL)");

            // delete orphan episodes, if any exist
            migrationBuilder.Sql(
                @"DELETE FROM MediaItem WHERE Id IN
                (SELECT mi.Id FROM Library l
                INNER JOIN LibraryPath lp ON l.Id = lp.LibraryId
                INNER JOIN MediaItem mi ON lp.Id = mi.LibraryPathId
                LEFT OUTER JOIN Show s ON mi.Id = s.Id
                LEFT OUTER JOIN Season ssn ON mi.Id = ssn.Id
                LEFT OUTER JOIN Episode e ON mi.Id = e.Id
                WHERE l.MediaKind = 2 AND s.Id IS NULL AND ssn.Id IS NULL AND e.Id IS NULL)");

            migrationBuilder.CreateIndex(
                "IX_MediaItem_Path",
                "MediaItem",
                "Path",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropIndex(
                "IX_MediaItem_Path",
                "MediaItem");
    }
}
