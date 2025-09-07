using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Populate_PlayoutHistory_Finish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE PlayoutHistory
                    SET Finish =
                            (SELECT PlayoutItem.Finish
                             FROM PlayoutItem
                             WHERE PlayoutItem.PlayoutId = PlayoutHistory.PlayoutId
                               AND PlayoutItem.Start = PlayoutHistory.`When`)
                    WHERE EXISTS (SELECT 1
                                  FROM PlayoutItem
                                  WHERE PlayoutItem.PlayoutId = PlayoutHistory.PlayoutId
                                    AND PlayoutItem.Start = PlayoutHistory.`When`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
