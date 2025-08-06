using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutItemWatermarkTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayoutItemWatermark",
                columns: table => new
                {
                    PlayoutItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    WatermarkId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutItemWatermark", x => new { x.PlayoutItemId, x.WatermarkId });
                    table.ForeignKey(
                        name: "FK_PlayoutItemWatermark_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutItemWatermark_PlayoutItem_PlayoutItemId",
                        column: x => x.PlayoutItemId,
                        principalTable: "PlayoutItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItemWatermark_WatermarkId",
                table: "PlayoutItemWatermark",
                column: "WatermarkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutItemWatermark");
        }
    }
}
