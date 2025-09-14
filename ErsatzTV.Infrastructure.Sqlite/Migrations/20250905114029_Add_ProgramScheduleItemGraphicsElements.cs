using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
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
                    ProgramScheduleItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    GraphicsElementId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleItemGraphicsElement", x => new { x.ProgramScheduleItemId, x.GraphicsElementId });
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemGraphicsElement_GraphicsElement_GraphicsElementId",
                        column: x => x.GraphicsElementId,
                        principalTable: "GraphicsElement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleItemGraphicsElement_ProgramScheduleItem_ProgramScheduleItemId",
                        column: x => x.ProgramScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
