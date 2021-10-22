using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_FillerPreset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.AddColumn<int>(
                name: "FallbackFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MidRollFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostRollFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreRollFillerId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FillerPreset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    FillerKind = table.Column<int>(type: "INTEGER", nullable: false),
                    FillerMode = table.Column<int>(type: "INTEGER", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: true),
                    PadToNearestMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    CollectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    MediaItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillerPreset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FillerPreset_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FillerPreset_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_FallbackFillerId",
                table: "ProgramScheduleItem",
                column: "FallbackFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MidRollFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_PostRollFillerId",
                table: "ProgramScheduleItem",
                column: "PostRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_PreRollFillerId",
                table: "ProgramScheduleItem",
                column: "PreRollFillerId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_CollectionId",
                table: "FillerPreset",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_MediaItemId",
                table: "FillerPreset",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_MultiCollectionId",
                table: "FillerPreset",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_SmartCollectionId",
                table: "FillerPreset",
                column: "SmartCollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailSmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem",
                column: "FallbackFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem",
                column: "MidRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem",
                column: "PostRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem",
                column: "PreRollFillerId",
                principalTable: "FillerPreset",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_FallbackFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_MidRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PostRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_FillerPreset_PreRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropTable(
                name: "FillerPreset");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_FallbackFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_MidRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_PostRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_PreRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "FallbackFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MidRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PostRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PreRollFillerId",
                table: "ProgramScheduleItem");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleDurationItem_SmartCollection_TailSmartCollectionId",
                table: "ProgramScheduleDurationItem",
                column: "TailSmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_SmartCollection_SmartCollectionId",
                table: "ProgramScheduleItem",
                column: "SmartCollectionId",
                principalTable: "SmartCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
