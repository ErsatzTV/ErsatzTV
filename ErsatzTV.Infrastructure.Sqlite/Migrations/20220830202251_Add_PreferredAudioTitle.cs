using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_PreferredAudioTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredAudioTitle",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredAudioTitle",
                table: "PlayoutItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredAudioTitle",
                table: "Channel",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredAudioTitle",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PreferredAudioTitle",
                table: "PlayoutItem");

            migrationBuilder.DropColumn(
                name: "PreferredAudioTitle",
                table: "Channel");
        }
    }
}
