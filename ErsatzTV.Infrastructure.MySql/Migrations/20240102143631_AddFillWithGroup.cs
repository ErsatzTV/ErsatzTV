using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddFillWithGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FillWithGroupMode",
                table: "ProgramScheduleItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayoutScheduleItemFillGroupIndex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    ProgramScheduleItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutScheduleItemFillGroupIndex", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutScheduleItemFillGroupIndex_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutScheduleItemFillGroupIndex_ProgramScheduleItem_Progra~",
                        column: x => x.ProgramScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FillGroupEnumeratorState",
                columns: table => new
                {
                    PlayoutScheduleItemFillGroupIndexId = table.Column<int>(type: "int", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillGroupEnumeratorState", x => x.PlayoutScheduleItemFillGroupIndexId);
                    table.ForeignKey(
                        name: "FK_FillGroupEnumeratorState_PlayoutScheduleItemFillGroupIndex_P~",
                        column: x => x.PlayoutScheduleItemFillGroupIndexId,
                        principalTable: "PlayoutScheduleItemFillGroupIndex",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutScheduleItemFillGroupIndex_PlayoutId",
                table: "PlayoutScheduleItemFillGroupIndex",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutScheduleItemFillGroupIndex_ProgramScheduleItemId",
                table: "PlayoutScheduleItemFillGroupIndex",
                column: "ProgramScheduleItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FillGroupEnumeratorState");

            migrationBuilder.DropTable(
                name: "PlayoutScheduleItemFillGroupIndex");

            migrationBuilder.DropColumn(
                name: "FillWithGroupMode",
                table: "ProgramScheduleItem");
        }
    }
}
