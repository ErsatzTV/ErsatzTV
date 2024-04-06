using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_DecoMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackCollectionId",
                table: "Deco",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackCollectionType",
                table: "Deco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackMediaItemId",
                table: "Deco",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackMode",
                table: "Deco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackMultiCollectionId",
                table: "Deco",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeadAirFallbackSmartCollectionId",
                table: "Deco",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WatermarkMode",
                table: "Deco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DeadAirFallbackCollectionId",
                table: "Deco",
                column: "DeadAirFallbackCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DeadAirFallbackMediaItemId",
                table: "Deco",
                column: "DeadAirFallbackMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DeadAirFallbackMultiCollectionId",
                table: "Deco",
                column: "DeadAirFallbackMultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DeadAirFallbackSmartCollectionId",
                table: "Deco",
                column: "DeadAirFallbackSmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_Collection_DeadAirFallbackCollectionId",
                table: "Deco",
                column: "DeadAirFallbackCollectionId",
                principalTable: "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_MediaItem_DeadAirFallbackMediaItemId",
                table: "Deco",
                column: "DeadAirFallbackMediaItemId",
                principalTable: "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_MultiCollection_DeadAirFallbackMultiCollectionId",
                table: "Deco",
                column: "DeadAirFallbackMultiCollectionId",
                principalTable: "MultiCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_SmartCollection_DeadAirFallbackSmartCollectionId",
                table: "Deco",
                column: "DeadAirFallbackSmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deco_Collection_DeadAirFallbackCollectionId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_MediaItem_DeadAirFallbackMediaItemId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_MultiCollection_DeadAirFallbackMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropForeignKey(
                name: "FK_Deco_SmartCollection_DeadAirFallbackSmartCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DeadAirFallbackCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DeadAirFallbackMediaItemId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DeadAirFallbackMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_DeadAirFallbackSmartCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackCollectionType",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackMediaItemId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackMode",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackMultiCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "DeadAirFallbackSmartCollectionId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "WatermarkMode",
                table: "Deco");
        }
    }
}
