using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_DecoGraphicsElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GraphicsElementsMode",
                table: "Deco",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseGraphicsElementsDuringFiller",
                table: "Deco",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DecoGraphicsElement",
                columns: table => new
                {
                    DecoId = table.Column<int>(type: "int", nullable: false),
                    GraphicsElementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoGraphicsElement", x => new { x.DecoId, x.GraphicsElementId });
                    table.ForeignKey(
                        name: "FK_DecoGraphicsElement_Deco_DecoId",
                        column: x => x.DecoId,
                        principalTable: "Deco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoGraphicsElement_GraphicsElement_GraphicsElementId",
                        column: x => x.GraphicsElementId,
                        principalTable: "GraphicsElement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DecoGraphicsElement_GraphicsElementId",
                table: "DecoGraphicsElement",
                column: "GraphicsElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecoGraphicsElement");

            migrationBuilder.DropColumn(
                name: "GraphicsElementsMode",
                table: "Deco");

            migrationBuilder.DropColumn(
                name: "UseGraphicsElementsDuringFiller",
                table: "Deco");
        }
    }
}
