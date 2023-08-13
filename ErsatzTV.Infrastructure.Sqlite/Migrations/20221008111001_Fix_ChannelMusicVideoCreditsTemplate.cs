using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Fix_ChannelMusicVideoCreditsTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Channel SET MusicVideoCreditsMode = 1 WHERE MusicVideoCreditsMode = 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
