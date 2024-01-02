using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
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
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayoutScheduleItemFillGroupIndex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramScheduleItemId = table.Column<int>(type: "INTEGER", nullable: false)
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
                        name: "FK_PlayoutScheduleItemFillGroupIndex_ProgramScheduleItem_ProgramScheduleItemId",
                        column: x => x.ProgramScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FillGroupEnumeratorState",
                columns: table => new
                {
                    PlayoutScheduleItemFillGroupIndexId = table.Column<int>(type: "INTEGER", nullable: false),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillGroupEnumeratorState", x => x.PlayoutScheduleItemFillGroupIndexId);
                    table.ForeignKey(
                        name: "FK_FillGroupEnumeratorState_PlayoutScheduleItemFillGroupIndex_PlayoutScheduleItemFillGroupIndexId",
                        column: x => x.PlayoutScheduleItemFillGroupIndexId,
                        principalTable: "PlayoutScheduleItemFillGroupIndex",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
