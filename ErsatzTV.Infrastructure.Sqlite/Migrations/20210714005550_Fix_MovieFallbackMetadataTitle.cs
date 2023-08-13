using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Fix_MovieFallbackMetadataTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE MovieMetadata SET Title = TRIM(Title), SortTitle = TRIM(SortTitle) WHERE MetadataKind = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
