using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_MusicVideoMetadata_Title : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE MusicVideoMetadata SET DateUpdated = '0001-01-01 00:00:00' WHERE MetadataKind = 0");

            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE LibraryId IN
                (SELECT Id FROM Library WHERE MediaKind = 3)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
