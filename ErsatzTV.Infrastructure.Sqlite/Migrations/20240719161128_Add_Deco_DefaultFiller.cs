using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_Deco_DefaultFiller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerCollectionId",
                table: "Deco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerCollectionType",
                table: "Deco",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerMediaItemId",
                table: "Deco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerMode",
                table: "Deco",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerMultiCollectionId",
                table: "Deco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultFillerSmartCollectionId",
                table: "Deco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DefaultFillerCollectionId",
                table: "Deco",
                column: "DefaultFillerCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DefaultFillerMediaItemId",
                table: "Deco",
                column: "DefaultFillerMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DefaultFillerMultiCollectionId",
                table: "Deco",
                column: "DefaultFillerMultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DefaultFillerSmartCollectionId",
                table: "Deco",
                column: "DefaultFillerSmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_Collection_DefaultFillerCollectionId",
                table: "Deco",
                column: "DefaultFillerCollectionId",
                principalTable: "Collection",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_MediaItem_DefaultFillerMediaItemId",
                table: "Deco",
                column: "DefaultFillerMediaItemId",
                principalTable: "MediaItem",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_MultiCollection_DefaultFillerMultiCollectionId",
                table: "Deco",
                column: "DefaultFillerMultiCollectionId",
                principalTable: "MultiCollection",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_SmartCollection_DefaultFillerSmartCollectionId",
                table: "Deco",
                column: "DefaultFillerSmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deco_Collection_DefaultFillerCollectionId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_MediaItem_DefaultFillerMediaItemId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_MultiCollection_DefaultFillerMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_SmartCollection_DefaultFillerSmartCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DefaultFillerCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DefaultFillerMediaItemId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DefaultFillerMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DefaultFillerSmartCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerCollectionType",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerMediaItemId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerMode",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DefaultFillerSmartCollectionId",
                table: "Deco");
        }
    }
}
