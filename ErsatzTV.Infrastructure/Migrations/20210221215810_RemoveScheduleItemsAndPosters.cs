using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class RemoveScheduleItemsAndPosters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // delete program schedule items that referenced television collections (that no longer exist)
            migrationBuilder.Sql(
                "delete from ProgramScheduleItems where MediaCollectionId not in (select Id from SimpleMediaCollections)");

            // delete television collections that no longer exist/work
            migrationBuilder.Sql(
                "delete from MediaCollections where Id not in (select Id from SimpleMediaCollections)");

            // delete all posters so they are all re-cached with a higher resolution
            migrationBuilder.Sql("update MediaItems set Poster = null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
