using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_ProgramScheduleItemMultipleMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MultipleMode",
                table: "ProgramScheduleMultipleItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE `ProgramScheduleMultipleItem` SET `MultipleMode` = 1 WHERE `Count` = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultipleMode",
                table: "ProgramScheduleMultipleItem");
        }
    }
}
