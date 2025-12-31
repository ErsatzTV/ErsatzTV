using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Optimize_JellyfinItemIdEtagColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JellyfinShow_ItemId",
                table: "JellyfinShow",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinSeason_ItemId",
                table: "JellyfinSeason",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinMovie_ItemId",
                table: "JellyfinMovie",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinLibrary_ItemId",
                table: "JellyfinLibrary",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinEpisode_ItemId",
                table: "JellyfinEpisode",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinCollection_ItemId",
                table: "JellyfinCollection",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JellyfinShow_ItemId",
                table: "JellyfinShow");

            migrationBuilder.DropIndex(
                name: "IX_JellyfinSeason_ItemId",
                table: "JellyfinSeason");

            migrationBuilder.DropIndex(
                name: "IX_JellyfinMovie_ItemId",
                table: "JellyfinMovie");

            migrationBuilder.DropIndex(
                name: "IX_JellyfinLibrary_ItemId",
                table: "JellyfinLibrary");

            migrationBuilder.DropIndex(
                name: "IX_JellyfinEpisode_ItemId",
                table: "JellyfinEpisode");

            migrationBuilder.DropIndex(
                name: "IX_JellyfinCollection_ItemId",
                table: "JellyfinCollection");
        }
    }
}
