using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Convert_FFmpegProfileFormats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // h264
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET VideoFormat = 1 WHERE VideoCodec LIKE '%264%'");

            // hevc
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET VideoFormat = 2 WHERE VideoCodec LIKE '%265%'");
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET VideoFormat = 2 WHERE VideoCodec LIKE '%hevc%'");

            // mpeg-2
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET VideoFormat = 3 WHERE VideoCodec LIKE '%mpeg2%'");

            // anything else => h264
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET VideoFormat = 1 WHERE VideoFormat = 0");

            // aac
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET AudioFormat = 1 WHERE AudioCodec LIKE '%AAC%'");

            // ac3
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET AudioFormat = 2 WHERE AudioCodec LIKE '%AC3%'");

            // anything else => aac
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET AudioFormat = 1 WHERE AudioFormat = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
