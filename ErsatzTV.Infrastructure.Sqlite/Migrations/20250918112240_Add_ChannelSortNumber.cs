using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_ChannelSortNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SortNumber",
                table: "Channel",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql(
                @"UPDATE `Channel`
                  SET `SortNumber` = CAST(`Number` AS REAL)
                  WHERE `Number` IS NOT NULL AND TRIM(`Number`) != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortNumber",
                table: "Channel");
        }
    }
}
