using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_FixH264VideoPreset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // clear video preset on all h264/10 bit ffmpeg profiles
            migrationBuilder.Sql("UPDATE `FFmpegProfile` SET `VideoPreset`='' WHERE `VideoFormat` = 1 AND `BitDepth` = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
