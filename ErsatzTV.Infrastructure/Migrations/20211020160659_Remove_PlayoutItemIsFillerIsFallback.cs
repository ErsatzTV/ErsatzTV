using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_PlayoutItemIsFillerIsFallback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFallback",
                table: "PlayoutItem");

            migrationBuilder.DropColumn(
                name: "IsFiller",
                table: "PlayoutItem");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFallback",
                table: "PlayoutItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFiller",
                table: "PlayoutItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
