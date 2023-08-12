using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ProgramScheduleItemSmartCollection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SmartCollectionId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_SmartCollectionId",
                table: "ProgramScheduleItem",
                column: "SmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_SmartCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "SmartCollectionId",
                table: "ProgramScheduleItem");
        }
    }
}
