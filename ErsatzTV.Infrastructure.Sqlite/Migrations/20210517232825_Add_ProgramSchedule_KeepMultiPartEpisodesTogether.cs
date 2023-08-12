using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_ProgramSchedule_KeepMultiPartEpisodesTogether : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<bool>(
                "KeepMultiPartEpisodesTogether",
                "ProgramSchedule",
                "INTEGER",
                nullable: false,
                defaultValue: false);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "KeepMultiPartEpisodesTogether",
                "ProgramSchedule");
    }
}
