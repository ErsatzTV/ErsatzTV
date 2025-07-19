using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_TraktListPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GeneratePlaylist",
                table: "TraktList",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "TraktList",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraktList_PlaylistId",
                table: "TraktList",
                column: "PlaylistId");

            migrationBuilder.AddForeignKey(
                name: "FK_TraktList_Playlist_PlaylistId",
                table: "TraktList",
                column: "PlaylistId",
                principalTable: "Playlist",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TraktList_Playlist_PlaylistId",
                table: "TraktList");

            migrationBuilder.DropIndex(
                name: "IX_TraktList_PlaylistId",
                table: "TraktList");

            migrationBuilder.DropColumn(
                name: "GeneratePlaylist",
                table: "TraktList");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "TraktList");
        }
    }
}
