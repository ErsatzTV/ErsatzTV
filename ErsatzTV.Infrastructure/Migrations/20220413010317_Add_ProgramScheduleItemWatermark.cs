using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ProgramScheduleItemWatermark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WatermarkId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_WatermarkId",
                table: "ProgramScheduleItem",
                column: "WatermarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_ChannelWatermark_WatermarkId",
                table: "ProgramScheduleItem",
                column: "WatermarkId",
                principalTable: "ChannelWatermark",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_ChannelWatermark_WatermarkId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_WatermarkId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "WatermarkId",
                table: "ProgramScheduleItem");
        }
    }
}
