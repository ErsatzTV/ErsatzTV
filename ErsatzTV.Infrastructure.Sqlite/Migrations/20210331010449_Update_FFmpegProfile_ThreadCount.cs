using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_FFmpegProfile_ThreadCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(@"UPDATE FFmpegProfile SET ThreadCount = 0 WHERE ThreadCount = 4");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
