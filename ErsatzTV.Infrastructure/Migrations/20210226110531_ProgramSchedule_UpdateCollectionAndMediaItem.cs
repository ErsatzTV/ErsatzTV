using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class ProgramSchedule_UpdateCollectionAndMediaItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // update schedule items that reference collections
            migrationBuilder.Sql(
                @"UPDATE ProgramScheduleItem SET CollectionId =
            (SELECT c.Id FROM Collection c INNER JOIN MediaCollection mc ON mc.Name = c.Name WHERE mc.Id = MediaCollectionId)
            WHERE MediaCollectionId > 0");

            // update schedule items that reference shows
            migrationBuilder.Sql(
                @"UPDATE ProgramScheduleItem SET MediaItemId =
            (SELECT mi.Id FROM MediaItem mi WHERE mi.TelevisionShowId = ProgramScheduleItem.TelevisionShowId)
            WHERE TelevisionShowId > 0");

            // update schedule items that reference seasons
            migrationBuilder.Sql(
                @"UPDATE ProgramScheduleItem SET MediaItemId =
            (SELECT mi.Id FROM MediaItem mi WHERE mi.TelevisionSeasonId = ProgramScheduleItem.TelevisionSeasonId)
            WHERE TelevisionSeasonId > 0");

            // update anchors that reference collections
            migrationBuilder.Sql(
                @"UPDATE PlayoutProgramScheduleAnchor SET NewCollectionId =
            (SELECT c.Id FROM Collection c INNER JOIN MediaCollection mc on c.Name = mc.Name WHERE mc.Id = CollectionId)
            WHERE CollectionType = 0");

            // update anchors that reference shows
            migrationBuilder.Sql(
                @"UPDATE PlayoutProgramScheduleAnchor SET MediaItemId =
            (SELECT mi.Id from MediaItem mi WHERE mi.TelevisionShowId = CollectionId)
            WHERE CollectionType = 1");

            // update anchors that reference seasons
            migrationBuilder.Sql(
                @"UPDATE PlayoutProgramScheduleAnchor SET MediaItemId =
            (SELECT mi.Id FROM MediaItem mi WHERE mi.TelevisionSeasonId = CollectionId)
            WHERE CollectionType = 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
