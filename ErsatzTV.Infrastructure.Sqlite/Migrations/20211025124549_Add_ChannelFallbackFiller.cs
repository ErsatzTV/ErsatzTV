using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ChannelFallbackFiller : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel");

            migrationBuilder.AddColumn<int>(
                name: "FallbackFillerId",
                table: "Channel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_FallbackFillerId",
                table: "Channel",
                column: "FallbackFillerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_FillerPreset_FallbackFillerId",
                table: "Channel",
                column: "FallbackFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "FK_Channel_FillerPreset_FallbackFillerId",
                table: "Channel");

            migrationBuilder.DropIndex(
                name: "IX_Channel_FallbackFillerId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "FallbackFillerId",
                table: "Channel");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
