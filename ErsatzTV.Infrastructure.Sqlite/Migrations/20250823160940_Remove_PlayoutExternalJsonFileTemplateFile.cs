using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Remove_PlayoutExternalJsonFileTemplateFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalJsonFile",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "TemplateFile",
                table: "Playout");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalJsonFile",
                table: "Playout",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateFile",
                table: "Playout",
                type: "TEXT",
                nullable: true);
        }
    }
}
