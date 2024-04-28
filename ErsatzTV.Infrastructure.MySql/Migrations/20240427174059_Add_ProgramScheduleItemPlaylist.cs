using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProgramScheduleItemPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "ProgramScheduleItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "PlayoutProgramScheduleAnchor",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_PlaylistId",
                table: "ProgramScheduleItem",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_PlaylistId",
                table: "PlayoutProgramScheduleAnchor",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_Playlist_PlaylistId",
                table: "PlayoutProgramScheduleAnchor",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_Playlist_PlaylistId",
                table: "ProgramScheduleItem",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_Playlist_PlaylistId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_Playlist_PlaylistId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_PlaylistId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutProgramScheduleAnchor_PlaylistId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
