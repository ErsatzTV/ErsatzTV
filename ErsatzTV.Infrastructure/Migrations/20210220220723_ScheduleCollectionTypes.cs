using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class ScheduleCollectionTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_MediaCollections_MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropIndex(
                "IX_PlayoutProgramScheduleItemAnchors_MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.RenameColumn(
                "MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors",
                "CollectionType");

            migrationBuilder.AlterColumn<int>(
                "MediaCollectionId",
                "ProgramScheduleItems",
                "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                "CollectionType",
                "ProgramScheduleItems",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "TelevisionSeasonId",
                "ProgramScheduleItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "TelevisionShowId",
                "ProgramScheduleItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                    "Id",
                    "PlayoutProgramScheduleItemAnchors",
                    "INTEGER",
                    nullable: false,
                    defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                "CollectionId",
                "PlayoutProgramScheduleItemAnchors",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors",
                "Id");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItems_TelevisionSeasonId",
                "ProgramScheduleItems",
                "TelevisionSeasonId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItems_TelevisionShowId",
                "ProgramScheduleItems",
                "TelevisionShowId");

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleItemAnchors_PlayoutId",
                "PlayoutProgramScheduleItemAnchors",
                "PlayoutId");

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems",
                "MediaCollectionId",
                "MediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_TelevisionSeasons_TelevisionSeasonId",
                "ProgramScheduleItems",
                "TelevisionSeasonId",
                "TelevisionSeasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_TelevisionShows_TelevisionShowId",
                "ProgramScheduleItems",
                "TelevisionShowId",
                "TelevisionShows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_TelevisionSeasons_TelevisionSeasonId",
                "ProgramScheduleItems");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItems_TelevisionShows_TelevisionShowId",
                "ProgramScheduleItems");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItems_TelevisionSeasonId",
                "ProgramScheduleItems");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItems_TelevisionShowId",
                "ProgramScheduleItems");

            migrationBuilder.DropPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropIndex(
                "IX_PlayoutProgramScheduleItemAnchors_PlayoutId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropColumn(
                "CollectionType",
                "ProgramScheduleItems");

            migrationBuilder.DropColumn(
                "TelevisionSeasonId",
                "ProgramScheduleItems");

            migrationBuilder.DropColumn(
                "TelevisionShowId",
                "ProgramScheduleItems");

            migrationBuilder.DropColumn(
                "Id",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.DropColumn(
                "CollectionId",
                "PlayoutProgramScheduleItemAnchors");

            migrationBuilder.RenameColumn(
                "CollectionType",
                "PlayoutProgramScheduleItemAnchors",
                "MediaCollectionId");

            migrationBuilder.AlterColumn<int>(
                "MediaCollectionId",
                "ProgramScheduleItems",
                "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                "PK_PlayoutProgramScheduleItemAnchors",
                "PlayoutProgramScheduleItemAnchors",
                new[] { "PlayoutId", "ProgramScheduleId", "MediaCollectionId" });

            migrationBuilder.CreateIndex(
                "IX_PlayoutProgramScheduleItemAnchors_MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors",
                "MediaCollectionId");

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleItemAnchors_MediaCollections_MediaCollectionId",
                "PlayoutProgramScheduleItemAnchors",
                "MediaCollectionId",
                "MediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItems_MediaCollections_MediaCollectionId",
                "ProgramScheduleItems",
                "MediaCollectionId",
                "MediaCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
