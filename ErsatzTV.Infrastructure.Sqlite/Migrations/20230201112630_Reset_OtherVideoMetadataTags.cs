using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class ResetOtherVideoMetadataTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE OtherVideoMetadata SET DateUpdated = '0001-01-01 00:00:00' WHERE OtherVideoId IN (SELECT OtherVideoId FROM OtherVideoMetadata WHERE MetadataKind = 1)");
            migrationBuilder.Sql("UPDATE LibraryFolder SET Etag = NULL WHERE Id IN (SELECT LF.Id FROM LibraryFolder LF INNER JOIN LibraryPath LP on LP.Id = LF.LibraryPathId INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 4)");
            migrationBuilder.Sql("UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN (SELECT LP.Id FROM LibraryPath LP INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 4)");
            migrationBuilder.Sql("UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE MediaKind = 4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
