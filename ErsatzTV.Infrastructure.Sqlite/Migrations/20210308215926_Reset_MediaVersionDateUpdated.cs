using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Reset_MediaVersionDateUpdated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(@"UPDATE MediaVersion SET DateUpdated = '0001-01-01 00:00:00'");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
