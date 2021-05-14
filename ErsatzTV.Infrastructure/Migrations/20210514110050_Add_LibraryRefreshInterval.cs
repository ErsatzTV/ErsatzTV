using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_LibraryRefreshInterval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO ConfigElement (Key, Value) VALUES ('scanner.library_refresh_interval', '6')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
