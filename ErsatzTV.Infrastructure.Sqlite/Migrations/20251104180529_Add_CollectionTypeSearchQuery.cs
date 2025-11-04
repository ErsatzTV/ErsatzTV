using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_CollectionTypeSearchQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchQuery",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchTitle",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchQuery",
                table: "PlayoutProgramScheduleAnchor",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchQuery",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "SearchTitle",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "SearchQuery",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
