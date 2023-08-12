using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_PlayoutProgramScheduleAnchorSmartCollection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "SmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_SmartCollection_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutProgramScheduleAnchor_SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "SmartCollectionId",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
