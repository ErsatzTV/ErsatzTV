using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MultiCollectionSmartCollection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MultiCollectionSmartItem",
                columns: table => new
                {
                    MultiCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SmartCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleAsGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlaybackOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollectionSmartItem", x => new { x.MultiCollectionId, x.SmartCollectionId });
                    table.ForeignKey(
                        name: "FK_MultiCollectionSmartItem_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiCollectionSmartItem_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiCollectionSmartItem_SmartCollectionId",
                table: "MultiCollectionSmartItem",
                column: "SmartCollectionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiCollectionSmartItem");
        }
    }
}
