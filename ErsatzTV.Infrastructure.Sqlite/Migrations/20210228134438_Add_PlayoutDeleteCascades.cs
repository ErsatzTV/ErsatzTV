using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_PlayoutDeleteCascades : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
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

            migrationBuilder.CreateIndex(
                "IX_MediaFile_Path",
                "MediaFile",
                "Path",
                unique: true);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
                "PlayoutProgramScheduleAnchor",
                "CollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_MediaItem_MediaItemId",
                "PlayoutProgramScheduleAnchor",
                "MediaItemId",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_Collection_CollectionId",
                "ProgramScheduleItem",
                "CollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_MediaItem_MediaItemId",
                "ProgramScheduleItem",
                "MediaItemId",
                "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
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
                "IX_MediaFile_Path",
                "MediaFile");

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
                "PlayoutProgramScheduleAnchor",
                "CollectionId",
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
    }
}
