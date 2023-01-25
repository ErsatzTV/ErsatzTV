using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _202301241316 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MidRollEnterFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MidRollExitFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MidRollEnterFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollEnterFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MidRollExitFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollExitFillerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollEnterFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollEnterFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollExitFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollExitFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollEnterFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollExitFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_MidRollEnterFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_MidRollExitFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MidRollEnterFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MidRollExitFillerId",
                table: "ProgramScheduleItem");
        }
    }
}
