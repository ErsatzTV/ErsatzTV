using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_MusicVideo_Directors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MusicVideoMetadataId",
                table: "Director",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Director_MusicVideoMetadataId",
                table: "Director",
                column: "MusicVideoMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Director_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Director",
                column: "MusicVideoMetadataId",
                principalTable: "MusicVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Director_MusicVideoMetadata_MusicVideoMetadataId",
                table: "Director");

            migrationBuilder.DropIndex(
                name: "IX_Director_MusicVideoMetadataId",
                table: "Director");

            migrationBuilder.DropColumn(
                name: "MusicVideoMetadataId",
                table: "Director");
        }
    }
}
