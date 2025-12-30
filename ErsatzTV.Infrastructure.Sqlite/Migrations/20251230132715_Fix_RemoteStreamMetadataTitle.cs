using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Fix_RemoteStreamMetadataTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // mark the file as needing an update
            migrationBuilder.Sql(
                "UPDATE `RemoteStreamMetadata` SET `DateUpdated` = '0001-01-01 00:00:00' WHERE `Title` IS NULL");

            // mark the folder as needing an update
            migrationBuilder.Sql(
                """
                UPDATE `LibraryFolder` SET `Etag` = NULL WHERE `Id` IN (
                    SELECT `MF`.`LibraryFolderId`
                    FROM `MediaFile` `MF`
                    INNER JOIN `MediaVersion` `MV` ON `MV`.`Id` = `MF`.`MediaVersionId`
                    INNER JOIN `RemoteStream` `RS` ON `RS`.`Id` = `MV`.`RemoteStreamId`
                    INNER JOIN `RemoteStreamMetadata` `RSM` ON `RSM`.`RemoteStreamId` = `RS`.`Id`
                    WHERE `RSM`.`Title` IS NULL
                    )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
