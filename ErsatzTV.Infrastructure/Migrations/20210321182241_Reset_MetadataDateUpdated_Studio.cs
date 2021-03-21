using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Reset_MetadataDateUpdated_Studio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(@"UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(@"UPDATE Library SET LastScan = '0001-01-01 00:00:00'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
