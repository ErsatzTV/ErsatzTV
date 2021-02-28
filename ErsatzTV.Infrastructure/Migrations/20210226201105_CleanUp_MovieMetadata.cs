using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class CleanUp_MovieMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_NewMovieMetadata_Movie_MovieId",
                "NewMovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_NewCollectionId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropPrimaryKey(
                "PK_NewMovieMetadata",
                "NewMovieMetadata");

            migrationBuilder.RenameTable(
                "NewMovieMetadata",
                newName: "MovieMetadata");

            migrationBuilder.RenameColumn(
                "NewCollectionId",
                "PlayoutProgramScheduleAnchor",
                "CollectionId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleAnchor_NewCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                newName: "IX_PlayoutProgramScheduleAnchor_CollectionId");

            migrationBuilder.RenameIndex(
                "IX_NewMovieMetadata_MovieId",
                table: "MovieMetadata",
                newName: "IX_MovieMetadata_MovieId");

            migrationBuilder.AddPrimaryKey(
                "PK_MovieMetadata",
                "MovieMetadata",
                "Id");

            migrationBuilder.AddForeignKey(
                "FK_MovieMetadata_Movie_MovieId",
                "MovieMetadata",
                "MovieId",
                "Movie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
                "PlayoutProgramScheduleAnchor",
                "CollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_MovieMetadata_Movie_MovieId",
                "MovieMetadata");

            migrationBuilder.DropForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_CollectionId",
                "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropPrimaryKey(
                "PK_MovieMetadata",
                "MovieMetadata");

            migrationBuilder.RenameTable(
                "MovieMetadata",
                newName: "NewMovieMetadata");

            migrationBuilder.RenameColumn(
                "CollectionId",
                "PlayoutProgramScheduleAnchor",
                "NewCollectionId");

            migrationBuilder.RenameIndex(
                "IX_PlayoutProgramScheduleAnchor_CollectionId",
                table: "PlayoutProgramScheduleAnchor",
                newName: "IX_PlayoutProgramScheduleAnchor_NewCollectionId");

            migrationBuilder.RenameIndex(
                "IX_MovieMetadata_MovieId",
                table: "NewMovieMetadata",
                newName: "IX_NewMovieMetadata_MovieId");

            migrationBuilder.AddPrimaryKey(
                "PK_NewMovieMetadata",
                "NewMovieMetadata",
                "Id");

            migrationBuilder.AddForeignKey(
                "FK_NewMovieMetadata_Movie_MovieId",
                "NewMovieMetadata",
                "MovieId",
                "Movie",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_PlayoutProgramScheduleAnchor_Collection_NewCollectionId",
                "PlayoutProgramScheduleAnchor",
                "NewCollectionId",
                "Collection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
