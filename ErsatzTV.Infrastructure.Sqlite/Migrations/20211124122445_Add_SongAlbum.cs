using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_SongAlbum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio");

            migrationBuilder.AddColumn<string>(
                name: "Album",
                table: "SongMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "SongMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "SongMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Track",
                table: "SongMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio");

            migrationBuilder.DropColumn(
                name: "Album",
                table: "SongMetadata");

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "SongMetadata");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "SongMetadata");

            migrationBuilder.DropColumn(
                name: "Track",
                table: "SongMetadata");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_SongMetadata_SongMetadataId",
                table: "Genre",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_SongMetadata_SongMetadataId",
                table: "Studio",
                column: "SongMetadataId",
                principalTable: "SongMetadata",
                principalColumn: "Id");
        }
    }
}
