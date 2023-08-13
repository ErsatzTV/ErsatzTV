using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ProgramScheduleItemLanguageCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredAudioLanguageCode",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSubtitleLanguageCode",
                table: "ProgramScheduleItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubtitleMode",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredAudioLanguageCode",
                table: "PlayoutItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSubtitleLanguageCode",
                table: "PlayoutItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubtitleMode",
                table: "PlayoutItem",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredAudioLanguageCode",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PreferredSubtitleLanguageCode",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "SubtitleMode",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "PreferredAudioLanguageCode",
                table: "PlayoutItem");

            migrationBuilder.DropColumn(
                name: "PreferredSubtitleLanguageCode",
                table: "PlayoutItem");

            migrationBuilder.DropColumn(
                name: "SubtitleMode",
                table: "PlayoutItem");
        }
    }
}
