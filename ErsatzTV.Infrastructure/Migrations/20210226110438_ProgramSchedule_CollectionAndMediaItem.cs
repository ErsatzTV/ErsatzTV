using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class ProgramSchedule_CollectionAndMediaItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "CollectionId",
                "ProgramScheduleItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MediaItemId",
                "ProgramScheduleItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "MediaItemId",
                "PlayoutProgramScheduleAnchor",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "NewCollectionId",
                "PlayoutProgramScheduleAnchor",
                "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItem_CollectionId",
                "ProgramScheduleItem",
                "CollectionId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItem_MediaItemId",
                "ProgramScheduleItem",
                "MediaItemId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleAnchor_MediaItemId",
                "PlayoutProgramScheduleAnchor",
                "MediaItemId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleAnchor_NewCollectionId",
                "PlayoutProgramScheduleAnchor",
                "NewCollectionId");

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_NewCollectionId",
                "PlayoutProgramScheduleAnchor",
                "NewCollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_MediaItem_MediaItemId",
                "PlayoutProgramScheduleAnchor",
                "MediaItemId",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_Collection_CollectionId",
                "ProgramScheduleItem",
                "CollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_MediaItem_MediaItemId",
                "ProgramScheduleItem",
                "MediaItemId",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_NewCollectionId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_MediaItem_MediaItemId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_Collection_CollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_MediaItem_MediaItemId",
                "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItem_CollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItem_MediaItemId",
                "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                "IX_PlayoutProgramScheduleAnchor_MediaItemId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropIndex(
                "IX_PlayoutProgramScheduleAnchor_NewCollectionId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                "CollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "MediaItemId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "MediaItemId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                "NewCollectionId",
                "PlayoutProgramScheduleAnchor");
        }
    }
}
