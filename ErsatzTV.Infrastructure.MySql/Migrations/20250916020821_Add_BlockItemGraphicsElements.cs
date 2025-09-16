using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_BlockItemGraphicsElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockItemGraphicsElement",
                columns: table => new
                {
                    BlockItemId = table.Column<int>(type: "int", nullable: false),
                    GraphicsElementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockItemGraphicsElement", x => new { x.BlockItemId, x.GraphicsElementId });
                    table.ForeignKey(
                        name: "FK_BlockItemGraphicsElement_BlockItem_BlockItemId",
                        column: x => x.BlockItemId,
                        principalTable: "BlockItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockItemGraphicsElement_GraphicsElement_GraphicsElementId",
                        column: x => x.GraphicsElementId,
                        principalTable: "GraphicsElement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlockItemWatermark",
                columns: table => new
                {
                    BlockItemId = table.Column<int>(type: "int", nullable: false),
                    WatermarkId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockItemWatermark", x => new { x.BlockItemId, x.WatermarkId });
                    table.ForeignKey(
                        name: "FK_BlockItemWatermark_BlockItem_BlockItemId",
                        column: x => x.BlockItemId,
                        principalTable: "BlockItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockItemWatermark_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BlockItemGraphicsElement_GraphicsElementId",
                table: "BlockItemGraphicsElement",
                column: "GraphicsElementId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockItemWatermark_WatermarkId",
                table: "BlockItemWatermark",
                column: "WatermarkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockItemGraphicsElement");

            migrationBuilder.DropTable(
                name: "BlockItemWatermark");
        }
    }
}
