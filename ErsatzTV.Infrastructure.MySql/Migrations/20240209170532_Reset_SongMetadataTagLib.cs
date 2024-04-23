using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Reset_SongMetadataTagLib : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00' WHERE Id IN
                (SELECT A.lpid FROM
                (SELECT LP.Id AS lpid FROM LibraryPath LP INNER JOIN Library L on L.Id = LP.LibraryId WHERE MediaKind = 5)
                as A)");

            migrationBuilder.Sql(
                @"UPDATE Library SET LastScan = '0001-01-01 00:00:00' WHERE MediaKind = 5");

            migrationBuilder.Sql(
                @"UPDATE Artwork SET DateUpdated = '0001-01-01 00:00:00' WHERE SongMetadataId IS NOT NULL");
            
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE Id IN
                (SELECT A.lfid FROM
                (SELECT LF.Id AS lfid FROM LibraryFolder LF INNER JOIN LibraryPath LP on LF.LibraryPathId = LP.Id INNER JOIN Library L on LP.LibraryId = L.Id WHERE MediaKind = 5)
                AS A)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
