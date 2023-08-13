using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Move_ToOwnedTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playout_ProgramScheduleItem_Anchor_NextScheduleItemId",
                table: "Playout");

            migrationBuilder.DropIndex(
                name: "IX_Playout_Anchor_NextScheduleItemId",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "EnumeratorState_Index",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "EnumeratorState_Seed",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "Anchor_DurationFinish",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_InDurationFiller",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_InFlood",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_MultipleRemaining",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_NextGuideGroup",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_NextScheduleItemId",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "Anchor_NextStart",
                table: "Playout");

            migrationBuilder.CreateTable(
                name: "CollectionEnumeratorState",
                columns: table => new
                {
                    PlayoutProgramScheduleAnchorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Seed = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionEnumeratorState", x => x.PlayoutProgramScheduleAnchorId);
                    table.ForeignKey(
                        name: "FK_CollectionEnumeratorState_PlayoutProgramScheduleAnchor_PlayoutProgramScheduleAnchorId",
                        column: x => x.PlayoutProgramScheduleAnchorId,
                        principalTable: "PlayoutProgramScheduleAnchor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayoutAnchor",
                columns: table => new
                {
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    NextScheduleItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    NextStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MultipleRemaining = table.Column<int>(type: "INTEGER", nullable: true),
                    DurationFinish = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InFlood = table.Column<bool>(type: "INTEGER", nullable: false),
                    InDurationFiller = table.Column<bool>(type: "INTEGER", nullable: false),
                    NextGuideGroup = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutAnchor", x => x.PlayoutId);
                    table.ForeignKey(
                        name: "FK_PlayoutAnchor_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutAnchor_ProgramScheduleItem_NextScheduleItemId",
                        column: x => x.NextScheduleItemId,
                        principalTable: "ProgramScheduleItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutAnchor_NextScheduleItemId",
                table: "PlayoutAnchor",
                column: "NextScheduleItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionEnumeratorState");

            migrationBuilder.DropTable(
                name: "PlayoutAnchor");

            migrationBuilder.AddColumn<int>(
                name: "EnumeratorState_Index",
                table: "PlayoutProgramScheduleAnchor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnumeratorState_Seed",
                table: "PlayoutProgramScheduleAnchor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Anchor_DurationFinish",
                table: "Playout",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Anchor_InDurationFiller",
                table: "Playout",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Anchor_InFlood",
                table: "Playout",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Anchor_MultipleRemaining",
                table: "Playout",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Anchor_NextGuideGroup",
                table: "Playout",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Anchor_NextScheduleItemId",
                table: "Playout",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Anchor_NextStart",
                table: "Playout",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Playout_Anchor_NextScheduleItemId",
                table: "Playout",
                column: "Anchor_NextScheduleItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Playout_ProgramScheduleItem_Anchor_NextScheduleItemId",
                table: "Playout",
                column: "Anchor_NextScheduleItemId",
                principalTable: "ProgramScheduleItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
