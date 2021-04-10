using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_LibraryLastScan_Metadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (SELECT LP.Id FROM LibraryPath LP INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 2)");

            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE MediaKind = 2");

            migrationBuilder.Sql(
                @"UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00' WHERE MetadataKind = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
