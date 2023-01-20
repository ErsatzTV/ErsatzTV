using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnchorSmartCollectionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id");
        }
    }
}
