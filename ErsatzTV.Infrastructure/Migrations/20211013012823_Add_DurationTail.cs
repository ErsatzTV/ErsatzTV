using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_DurationTail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfflineTail",
                table: "ProgramScheduleDurationItem",
                newName: "TailMode");

            migrationBuilder.AddColumn<int>(
                name: "TailCollectionId",
                table: "ProgramScheduleDurationItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TailCollectionType",
                table: "ProgramScheduleDurationItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TailMediaItemId",
                table: "ProgramScheduleDurationItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TailMultiCollectionId",
                table: "ProgramScheduleDurationItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TailSmartCollectionId",
                table: "ProgramScheduleDurationItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleDurationItem_TailCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleDurationItem_TailMediaItemId",
                table: "ProgramScheduleDurationItem",
                column: "TailMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleDurationItem_TailMultiCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailMultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleDurationItem_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailSmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_Collection_TailCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailCollectionId",
                principalTable: "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_MediaItem_TailMediaItemId",
                table: "ProgramScheduleDurationItem",
                column: "TailMediaItemId",
                principalTable: "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_MultiCollection_TailMultiCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailMultiCollectionId",
                principalTable: "MultiCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailSmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_Collection_TailCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_MediaItem_TailMediaItemId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_MultiCollection_TailMultiCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleDurationItem_TailCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleDurationItem_TailMediaItemId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleDurationItem_TailMultiCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleDurationItem_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropColumn(
                name: "TailCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropColumn(
                name: "TailCollectionType",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropColumn(
                name: "TailMediaItemId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropColumn(
                name: "TailMultiCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropColumn(
                name: "TailSmartCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.RenameColumn(
                name: "TailMode",
                table: "ProgramScheduleDurationItem",
                newName: "OfflineTail");
        }
    }
}
