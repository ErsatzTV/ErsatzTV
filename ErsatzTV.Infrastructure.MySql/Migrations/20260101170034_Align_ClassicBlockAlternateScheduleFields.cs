using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Align_ClassicBlockAlternateScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EndDay",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndMonth",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndYear",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LimitToDateRange",
                table: "ProgramScheduleAlternate",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartDay",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartMonth",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartYear",
                table: "ProgramScheduleAlternate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndYear",
                table: "PlayoutTemplate",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartYear",
                table: "PlayoutTemplate",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDay",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "EndYear",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "LimitToDateRange",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "StartDay",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "StartYear",
                table: "ProgramScheduleAlternate");

            migrationBuilder.DropColumn(
                name: "EndYear",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "StartYear",
                table: "PlayoutTemplate");
        }
    }
}
