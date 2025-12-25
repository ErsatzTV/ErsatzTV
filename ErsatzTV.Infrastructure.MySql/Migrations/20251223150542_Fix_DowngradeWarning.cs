using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Fix_DowngradeWarning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // this migration was removed because it failed to apply successfully on MySql
            // nonetheless, it succeeded on MariaDB so some users still have this history record
            // which causes an erroneous "downgrade" warning (db has migration that app doesn't know about)
            // so it needs to be cleaned up
            migrationBuilder.Sql(
                "DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250723030616_Update_MediaFilePath'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
