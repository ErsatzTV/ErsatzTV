using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_FillerPresetPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "FillerPreset",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FillerPreset_PlaylistId",
                table: "FillerPreset",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_FillerPreset_Playlist_PlaylistId",
                table: "FillerPreset",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FillerPreset_Playlist_PlaylistId",
                table: "FillerPreset");

            migrationBuilder.DropIndex(
                name: "IX_FillerPreset_PlaylistId",
                table: "FillerPreset");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "FillerPreset");
        }
    }
}
