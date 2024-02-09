using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Update_SongMetadataParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "SongMetadata",
                newName: "Comment");

            migrationBuilder.RenameColumn(
                name: "Artist",
                table: "SongMetadata",
                newName: "Artists");

            migrationBuilder.RenameColumn(
                name: "AlbumArtist",
                table: "SongMetadata",
                newName: "AlbumArtists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "SongMetadata",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "Artists",
                table: "SongMetadata",
                newName: "Artist");

            migrationBuilder.RenameColumn(
                name: "AlbumArtists",
                table: "SongMetadata",
                newName: "AlbumArtist");
        }
    }
}
