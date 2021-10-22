using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ProgramScheduleItem_TailFiller : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.AddColumn<int>(
                name: "TailFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_TailFillerId",
                table: "ProgramScheduleItem",
                column: "TailFillerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem",
                column: "FallbackFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem",
                column: "PostRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem",
                column: "PreRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_TailFillerId",
                table: "ProgramScheduleItem",
                column: "TailFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_TailFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_TailFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "TailFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem",
                column: "FallbackFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem",
                column: "PostRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem",
                column: "PreRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
