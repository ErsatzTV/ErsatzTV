using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_ProgramScheduleMediaCollectionPlaybackOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaCollectionPlaybackOrder",
                table: "ProgramSchedule");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MediaCollectionPlaybackOrder",
                table: "ProgramSchedule",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
