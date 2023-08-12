using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_JellyfinArtworkPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                @"UPDATE Artwork SET Path = REPLACE(Path, 'jellyfin:///Items', 'jellyfin://Items')
                  WHERE Path LIKE 'jellyfin:///Items%'");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
