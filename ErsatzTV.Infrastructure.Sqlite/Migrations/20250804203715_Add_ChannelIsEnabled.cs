using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ChannelIsEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Channel",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInEpg",
                table: "Channel",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE `Channel` SET `IsEnabled` = 0 WHERE `ActiveMode` = 2");
            migrationBuilder.Sql("UPDATE `Channel` SET `ShowInEpg` = 0 WHERE `ActiveMode` = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "ShowInEpg",
                table: "Channel");
        }
    }
}
