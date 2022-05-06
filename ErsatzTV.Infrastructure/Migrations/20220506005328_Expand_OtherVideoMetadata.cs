using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Expand_OtherVideoMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio");

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Writer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentRating",
                table: "OtherVideoMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outline",
                table: "OtherVideoMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plot",
                table: "OtherVideoMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "OtherVideoMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtherVideoMetadataId",
                table: "Director",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Writer_OtherVideoMetadataId",
                table: "Writer",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Director_OtherVideoMetadataId",
                table: "Director",
                column: "OtherVideoMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Director_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Director",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Writer_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Writer",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor");

            migrationBuilder.DropForeignKey(
                name: "FK_Director_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Director");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid");

            migrationBuilder.DropForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio");

            migrationBuilder.DropForeignKey(
                name: "FK_Writer_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Writer");

            migrationBuilder.DropIndex(
                name: "IX_Writer_OtherVideoMetadataId",
                table: "Writer");

            migrationBuilder.DropIndex(
                name: "IX_Director_OtherVideoMetadataId",
                table: "Director");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Writer");

            migrationBuilder.DropColumn(
                name: "ContentRating",
                table: "OtherVideoMetadata");

            migrationBuilder.DropColumn(
                name: "Outline",
                table: "OtherVideoMetadata");

            migrationBuilder.DropColumn(
                name: "Plot",
                table: "OtherVideoMetadata");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "OtherVideoMetadata");

            migrationBuilder.DropColumn(
                name: "OtherVideoMetadataId",
                table: "Director");

            migrationBuilder.AddForeignKey(
                name: "FK_Actor_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Actor",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Genre",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MetadataGuid_OtherVideoMetadata_OtherVideoMetadataId",
                table: "MetadataGuid",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Studio_OtherVideoMetadata_OtherVideoMetadataId",
                table: "Studio",
                column: "OtherVideoMetadataId",
                principalTable: "OtherVideoMetadata",
                principalColumn: "Id");
        }
    }
}
