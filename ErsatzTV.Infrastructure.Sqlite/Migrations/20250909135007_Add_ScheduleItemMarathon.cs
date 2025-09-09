using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ScheduleItemMarathon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MarathonBatchSize",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarathonGroupBy",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MarathonShuffleGroups",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarathonBatchSize",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MarathonGroupBy",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MarathonShuffleGroups",
                table: "ProgramScheduleItem");
        }
    }
}
