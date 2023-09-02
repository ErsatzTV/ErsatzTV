using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_MediaVersionDateAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
            migrationBuilder.Sql($@"UPDATE MediaVersion SET DateAdded = '{now}'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
