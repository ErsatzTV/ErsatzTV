using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ProgramScheduleItemPlaybackOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaybackOrder",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE ProgramScheduleItem SET PlaybackOrder = (SELECT MediaCollectionPlaybackOrder FROM ProgramSchedule WHERE ProgramSchedule.Id = ProgramScheduleItem.ProgramScheduleId)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaybackOrder",
                table: "ProgramScheduleItem");
        }
    }
}
