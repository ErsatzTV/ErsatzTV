using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Rework_LibraryFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LibraryFolderId",
                table: "MediaFile",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "LibraryFolder",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_LibraryFolderId",
                table: "MediaFile",
                column: "LibraryFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryFolder_ParentId",
                table: "LibraryFolder",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibraryFolder_LibraryFolder_ParentId",
                table: "LibraryFolder",
                column: "ParentId",
                principalTable: "LibraryFolder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFile_LibraryFolder_LibraryFolderId",
                table: "MediaFile",
                column: "LibraryFolderId",
                principalTable: "LibraryFolder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibraryFolder_LibraryFolder_ParentId",
                table: "LibraryFolder");

            migrationBuilder.DropForeignKey(
                name: "FK_MediaFile_LibraryFolder_LibraryFolderId",
                table: "MediaFile");

            migrationBuilder.DropIndex(
                name: "IX_MediaFile_LibraryFolderId",
                table: "MediaFile");

            migrationBuilder.DropIndex(
                name: "IX_LibraryFolder_ParentId",
                table: "LibraryFolder");

            migrationBuilder.DropColumn(
                name: "LibraryFolderId",
                table: "MediaFile");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "LibraryFolder");
        }
    }
}
