using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProgramScheduleItemWatermarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramScheduleItemWatermark",
                columns: table => new
                {
                    ProgramScheduleItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    WatermarkId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleItemWatermark", x => new { x.ProgramScheduleItemId, x.WatermarkId });
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemWatermark_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemWatermark_ProgramScheduleItem_ProgramScheduleItemId",
                        column: x => x.ProgramScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItemWatermark_WatermarkId",
                table: "ProgramScheduleItemWatermark",
                column: "WatermarkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramScheduleItemWatermark");
        }
    }
}
