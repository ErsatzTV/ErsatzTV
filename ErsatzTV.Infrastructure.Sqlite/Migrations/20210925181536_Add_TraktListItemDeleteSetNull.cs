using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_TraktListItemDeleteSetNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TraktListItem_MediaItem_MediaItemId",
                table: "TraktListItem");

            migrationBuilder.AddForeignKey(
                name: "FK_TraktListItem_MediaItem_MediaItemId",
                table: "TraktListItem",
                column: "MediaItemId",
                principalTable: "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TraktListItem_MediaItem_MediaItemId",
                table: "TraktListItem");

            migrationBuilder.AddForeignKey(
                name: "FK_TraktListItem_MediaItem_MediaItemId",
                table: "TraktListItem",
                column: "MediaItemId",
                principalTable: "MediaItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
