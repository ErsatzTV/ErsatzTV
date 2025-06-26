using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_ChannelStreamSelector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioStreamSelector",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "AudioStreamSelectorMode",
                table: "Channel");

            migrationBuilder.RenameColumn(
                name: "SubtitleStreamSelectorMode",
                table: "Channel",
                newName: "StreamSelectorMode");

            migrationBuilder.RenameColumn(
                name: "SubtitleStreamSelector",
                table: "Channel",
                newName: "StreamSelector");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StreamSelectorMode",
                table: "Channel",
                newName: "SubtitleStreamSelectorMode");

            migrationBuilder.RenameColumn(
                name: "StreamSelector",
                table: "Channel",
                newName: "SubtitleStreamSelector");

            migrationBuilder.AddColumn<string>(
                name: "AudioStreamSelector",
                table: "Channel",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudioStreamSelectorMode",
                table: "Channel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
