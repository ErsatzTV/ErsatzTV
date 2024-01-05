using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddFakeCollectionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FakeCollectionKey",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FakeCollectionKey",
                table: "PlayoutProgramScheduleAnchor",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FakeCollectionKey",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "FakeCollectionKey",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
