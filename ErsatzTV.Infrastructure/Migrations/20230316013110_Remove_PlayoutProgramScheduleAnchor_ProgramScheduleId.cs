using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Remove_PlayoutProgramScheduleAnchor_ProgramScheduleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_ProgramSchedule_ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutProgramScheduleAnchor_ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor",
                column: "ProgramScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_ProgramSchedule_ProgramScheduleId",
                table: "PlayoutProgramScheduleAnchor",
                column: "ProgramScheduleId",
                principalTable: "ProgramSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
