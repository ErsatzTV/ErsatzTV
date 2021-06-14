using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ChannelWatermarkNameImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel");

            migrationBuilder.DropIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "ChannelWatermark",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ChannelWatermark",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel",
                column: "WatermarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel");

            migrationBuilder.DropIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "ChannelWatermark");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ChannelWatermark");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_ChannelWatermark_WatermarkId",
                table: "Channel",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
