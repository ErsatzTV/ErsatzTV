using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProgramScheduleItemGraphicsElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramScheduleItemGraphicsElement",
                columns: table => new
                {
                    ProgramScheduleItemId = table.Column<int>(type: "int", nullable: false),
                    GraphicsElementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleItemGraphicsElement", x => new { x.ProgramScheduleItemId, x.GraphicsElementId });
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemGraphicsElement_GraphicsElement_GraphicsE~",
                        column: x => x.GraphicsElementId,
                        principalTable: "GraphicsElement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemGraphicsElement_ProgramScheduleItem_Progr~",
                        column: x => x.ProgramScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItemGraphicsElement_GraphicsElementId",
                table: "ProgramScheduleItemGraphicsElement",
                column: "GraphicsElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramScheduleItemGraphicsElement");
        }
    }
}
