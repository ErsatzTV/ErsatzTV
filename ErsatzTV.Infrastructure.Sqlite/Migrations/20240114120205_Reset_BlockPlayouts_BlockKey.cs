using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Reset_BlockPlayouts_BlockKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM PlayoutHistory");
            migrationBuilder.Sql(
                """
                DELETE FROM PlayoutItem
                WHERE PlayoutId IN (SELECT Id FROM Playout WHERE ProgramSchedulePlayoutType = 2)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
