using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class MediaItemPoster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "GenericIntegerIds");

            migrationBuilder.DropTable(
                "MediaCollectionSummaries");

            migrationBuilder.AddColumn<string>(
                "Poster",
                "MediaItems",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Poster",
                "MediaItems");

            migrationBuilder.CreateTable(
                "GenericIntegerIds",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table => { });

            migrationBuilder.CreateTable(
                "MediaCollectionSummaries",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false),
                    IsSimple = table.Column<bool>("INTEGER", nullable: false),
                    ItemCount = table.Column<int>("INTEGER", nullable: false),
                    Name = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { });
        }
    }
}
