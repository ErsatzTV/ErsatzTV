using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Populate_ProgramScheduleItemWatermarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO `ProgramScheduleItemWatermark` (`ProgramScheduleItemId`, `WatermarkId`)
                  SELECT `Id`, `WatermarkId` FROM `ProgramScheduleItem` WHERE `WatermarkId` IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
