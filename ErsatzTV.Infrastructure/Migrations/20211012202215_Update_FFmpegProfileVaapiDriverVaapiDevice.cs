using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_FFmpegProfileVaapiDriverVaapiDevice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE FFmpegProfile SET VaapiDevice = '/dev/dri/renderD128'");
            migrationBuilder.Sql(
                "UPDATE FFmpegProfile SET VaapiDriver = (SELECT IFNULL((SELECT Value AS A FROM ConfigElement WHERE Key = 'ffmpeg.vaapi_driver'), 0))");
            migrationBuilder.Sql("DELETE FROM ConfigElement WHERE Key = 'ffmpeg.vaapi_driver'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
