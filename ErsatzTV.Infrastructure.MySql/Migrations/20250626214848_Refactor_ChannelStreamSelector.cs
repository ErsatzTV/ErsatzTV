using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AudioStreamSelectorMode",
                table: "Channel",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
