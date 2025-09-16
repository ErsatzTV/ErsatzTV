using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ScheduleItemRerunCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RerunCollectionId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_RerunCollectionId",
                table: "ProgramScheduleItem",
                column: "RerunCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_RerunCollection_RerunCollectionId",
                table: "ProgramScheduleItem",
                column: "RerunCollectionId",
                principalTable: "RerunCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_RerunCollection_RerunCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_RerunCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "RerunCollectionId",
                table: "ProgramScheduleItem");
        }
    }
}
