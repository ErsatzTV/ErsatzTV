using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutAnchorRerunCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RerunCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_RerunCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "RerunCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_RerunCollection_RerunCollection~",
                table: "PlayoutProgramScheduleAnchor",
                column: "RerunCollectionId",
                principalTable: "RerunCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_RerunCollection_RerunCollection~",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutProgramScheduleAnchor_RerunCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "RerunCollectionId",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
