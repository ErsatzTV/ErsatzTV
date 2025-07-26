using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_MediaFilePathHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFile_Path",
                table: "MediaFile");

            migrationBuilder.AddColumn<string>(
                name: "PathHash",
                table: "MediaFile",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PathHash",
                table: "MediaFile");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFile_Path",
                table: "MediaFile",
                column: "Path",
                unique: true);
        }
    }
}
