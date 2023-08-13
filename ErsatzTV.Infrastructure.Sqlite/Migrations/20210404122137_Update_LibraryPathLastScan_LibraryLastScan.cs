using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_LibraryPathLastScan_LibraryLastScan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan =
                (SELECT LastScan FROM Library L
                INNER JOIN LocalLibrary LL on L.Id = LL.Id
                WHERE LibraryPath.LibraryId = L.Id)");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
