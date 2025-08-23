using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutScheduleFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScheduleFile",
                table: "Playout",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE `Playout` SET `ScheduleFile` = COALESCE(`ExternalJsonFile`, `TemplateFile`)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduleFile",
                table: "Playout");
        }
    }
}
