using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Fix_ChannelSortNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE `Channel`
                  SET `SortNumber` = CAST(`Number` AS REAL)
                  WHERE `Number` IS NOT NULL AND TRIM(`Number`) != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
