using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Populate_PlayoutHistory_Finish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlayoutItem_PlayoutId_Start",
                table: "PlayoutItem",
                columns: new[] { "PlayoutId", "Start" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutHistory_PlayoutId_When",
                table: "PlayoutHistory",
                columns: new[] { "PlayoutId", "When" });

            migrationBuilder.Sql(
                @"UPDATE PlayoutHistory
                    SET Finish = PlayoutItem.Finish
                    FROM PlayoutItem
                    WHERE PlayoutHistory.PlayoutId = PlayoutItem.PlayoutId
                      AND PlayoutHistory.`When` = PlayoutItem.Start;");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutItem_PlayoutId_Start",
                table: "PlayoutItem");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutHistory_PlayoutId_When",
                table: "PlayoutHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
