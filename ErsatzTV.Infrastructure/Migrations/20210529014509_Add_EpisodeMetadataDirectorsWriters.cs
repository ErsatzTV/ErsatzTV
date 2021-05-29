using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_EpisodeMetadataDirectorsWriters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EpisodeMetadataId",
                table: "Writer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EpisodeMetadataId",
                table: "Director",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Writer_EpisodeMetadataId",
                table: "Writer",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Director_EpisodeMetadataId",
                table: "Director",
                column: "EpisodeMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Director_EpisodeMetadata_EpisodeMetadataId",
                table: "Director",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Writer_EpisodeMetadata_EpisodeMetadataId",
                table: "Writer",
                column: "EpisodeMetadataId",
                principalTable: "EpisodeMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Director_EpisodeMetadata_EpisodeMetadataId",
                table: "Director");

            migrationBuilder.DropForeignKey(
                name: "FK_Writer_EpisodeMetadata_EpisodeMetadataId",
                table: "Writer");

            migrationBuilder.DropIndex(
                name: "IX_Writer_EpisodeMetadataId",
                table: "Writer");

            migrationBuilder.DropIndex(
                name: "IX_Director_EpisodeMetadataId",
                table: "Director");

            migrationBuilder.DropColumn(
                name: "EpisodeMetadataId",
                table: "Writer");

            migrationBuilder.DropColumn(
                name: "EpisodeMetadataId",
                table: "Director");
        }
    }
}
