using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ScheduleItemMarathonShuffleItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MarathonShuffleItems",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarathonShuffleItems",
                table: "ProgramScheduleItem");
        }
    }
}
