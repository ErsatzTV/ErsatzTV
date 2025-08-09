using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Remove_ProgramScheduleItemWatermark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
