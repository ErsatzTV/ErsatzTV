using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_BlockItemSearchQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchQuery",
                table: "BlockItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchTitle",
                table: "BlockItem",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchQuery",
                table: "BlockItem");

            migrationBuilder.DropColumn(
                name: "SearchTitle",
                table: "BlockItem");
        }
    }
}
