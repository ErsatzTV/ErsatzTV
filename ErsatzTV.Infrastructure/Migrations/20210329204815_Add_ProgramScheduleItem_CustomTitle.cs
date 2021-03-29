using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ProgramScheduleItem_CustomTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "CustomTitle",
                "ProgramScheduleItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                "CustomGroup",
                "PlayoutItem",
                "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                "CustomTitle",
                "PlayoutItem",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "CustomTitle",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "CustomGroup",
                "PlayoutItem");

            migrationBuilder.DropColumn(
                "CustomTitle",
                "PlayoutItem");
        }
    }
}
