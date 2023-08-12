using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class TurnOff_FrameRateNormalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE FFmpegProfile SET NormalizeFramerate = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
