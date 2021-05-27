using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MovieMetadataShowMetadataContentRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00' WHERE MetadataKind = 1");
            migrationBuilder.Sql("UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00' WHERE MetadataKind = 1");

            migrationBuilder.AddColumn<string>(
                name: "ContentRating",
                table: "ShowMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentRating",
                table: "MovieMetadata",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentRating",
                table: "ShowMetadata");

            migrationBuilder.DropColumn(
                name: "ContentRating",
                table: "MovieMetadata");
        }
    }
}
