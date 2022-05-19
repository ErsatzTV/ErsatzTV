using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlayoutItemWatermark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WatermarkId",
                table: "PlayoutItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItem_WatermarkId",
                table: "PlayoutItem",
                column: "WatermarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutItem_ChannelWatermark_WatermarkId",
                table: "PlayoutItem",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutItem_ChannelWatermark_WatermarkId",
                table: "PlayoutItem");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutItem_WatermarkId",
                table: "PlayoutItem");

            migrationBuilder.DropColumn(
                name: "WatermarkId",
                table: "PlayoutItem");
        }
    }
}
