using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_JellyfinShowSeasonEpisode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "JellyfinEpisode",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinEpisode", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinEpisode_Episode_Id",
                        x => x.Id,
                        "Episode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "JellyfinSeason",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinSeason", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinSeason_Season_Id",
                        x => x.Id,
                        "Season",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "JellyfinShow",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinShow", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinShow_Show_Id",
                        x => x.Id,
                        "Show",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "JellyfinEpisode");

            migrationBuilder.DropTable(
                "JellyfinSeason");

            migrationBuilder.DropTable(
                "JellyfinShow");
        }
    }
}
