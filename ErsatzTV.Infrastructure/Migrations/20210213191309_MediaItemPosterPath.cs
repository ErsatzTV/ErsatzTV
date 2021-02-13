using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class MediaItemPosterPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "GenericIntegerIds");

            migrationBuilder.DropTable(
                "MediaCollectionSummaries");

            migrationBuilder.AddColumn<string>(
                "PosterPath",
                "MediaItems",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "PosterPath",
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
