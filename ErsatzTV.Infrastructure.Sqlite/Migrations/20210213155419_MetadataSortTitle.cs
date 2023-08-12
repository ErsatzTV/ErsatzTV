using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class MetadataSortTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "Metadata_SortTitle",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE MediaItems
SET Metadata_SortTitle = Metadata_Title");

            migrationBuilder.Sql(
                @"UPDATE MediaItems
SET Metadata_SortTitle = substr(Metadata_Title, 5)
WHERE Metadata_Title LIKE 'the %'");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "Metadata_SortTitle",
                "MediaItems");
    }
}
