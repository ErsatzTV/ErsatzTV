using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_ChannelWatermarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Channel SET WatermarkId = NULL");
            migrationBuilder.Sql("DELETE FROM ChannelWatermark");
            migrationBuilder.Sql("DELETE FROM ConfigElement WHERE Key LIKE '%watermark%'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
