using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Remove_DecoWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deco_ChannelWatermark_WatermarkId",
                table: "Deco");

            migrationBuilder.DropIndex(
                name: "IX_Deco_WatermarkId",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "WatermarkId",
                table: "Deco");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WatermarkId",
                table: "Deco",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deco_WatermarkId",
                table: "Deco",
                column: "WatermarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deco_ChannelWatermark_WatermarkId",
                table: "Deco",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
