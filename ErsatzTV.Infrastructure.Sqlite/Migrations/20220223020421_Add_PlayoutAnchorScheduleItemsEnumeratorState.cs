using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_PlayoutAnchorScheduleItemsEnumeratorState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutAnchor_ProgramScheduleItem_NextScheduleItemId",
                table: "PlayoutAnchor");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutAnchor_NextScheduleItemId",
                table: "PlayoutAnchor");

            migrationBuilder.DropColumn(
                name: "NextScheduleItemId",
                table: "PlayoutAnchor");

            migrationBuilder.CreateTable(
                name: "ScheduleItemsEnumeratorState",
                columns: table => new
                {
                    PlayoutAnchorPlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleItemsEnumeratorState", x => x.PlayoutAnchorPlayoutId);
                    table.ForeignKey(
                        name: "FK_ScheduleItemsEnumeratorState_PlayoutAnchor_PlayoutAnchorPlayoutId",
                        column: x => x.PlayoutAnchorPlayoutId,
                        principalTable: "PlayoutAnchor",
                        principalColumn: "PlayoutId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleItemsEnumeratorState");

            migrationBuilder.AddColumn<int>(
                name: "NextScheduleItemId",
                table: "PlayoutAnchor",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutAnchor_NextScheduleItemId",
                table: "PlayoutAnchor",
                column: "NextScheduleItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutAnchor_ProgramScheduleItem_NextScheduleItemId",
                table: "PlayoutAnchor",
                column: "NextScheduleItemId",
                principalTable: "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
