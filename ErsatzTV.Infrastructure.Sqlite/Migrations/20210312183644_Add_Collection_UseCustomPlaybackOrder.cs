using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_Collection_UseCustomPlaybackOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<bool>(
                "UseCustomPlaybackOrder",
                "Collection",
                "INTEGER",
                nullable: false,
                defaultValue: false);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "UseCustomPlaybackOrder",
                "Collection");
    }
}
