using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutItemGraphicsElement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GraphicsElement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphicsElement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayoutItemGraphicsElement",
                columns: table => new
                {
                    PlayoutItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    GraphicsElementId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutItemGraphicsElement", x => new { x.PlayoutItemId, x.GraphicsElementId });
                    table.ForeignKey(
                        name: "FK_PlayoutItemGraphicsElement_GraphicsElement_GraphicsElementId",
                        column: x => x.GraphicsElementId,
                        principalTable: "GraphicsElement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutItemGraphicsElement_PlayoutItem_PlayoutItemId",
                        column: x => x.PlayoutItemId,
                        principalTable: "PlayoutItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItemGraphicsElement_GraphicsElementId",
                table: "PlayoutItemGraphicsElement",
                column: "GraphicsElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutItemGraphicsElement");

            migrationBuilder.DropTable(
                name: "GraphicsElement");
        }
    }
}
