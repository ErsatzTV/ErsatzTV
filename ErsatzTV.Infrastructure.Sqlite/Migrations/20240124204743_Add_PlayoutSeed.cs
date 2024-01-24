using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Seed",
                table: "PlayoutHistory",
                newName: "Index");

            migrationBuilder.AddColumn<int>(
                name: "Seed",
                table: "Playout",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Seed",
                table: "Playout");

            migrationBuilder.RenameColumn(
                name: "Index",
                table: "PlayoutHistory",
                newName: "Seed");
        }
    }
}
