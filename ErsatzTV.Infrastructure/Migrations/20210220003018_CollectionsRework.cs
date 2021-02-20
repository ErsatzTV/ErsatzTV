using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class CollectionsRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "MediaItemSimpleMediaCollection");

            migrationBuilder.DropTable(
                "TelevisionMediaCollections");

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionEpisodes",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionEpisodesId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionEpisodes",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionEpisodesId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionEpisodes_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionEpisodes_TelevisionEpisodes_TelevisionEpisodesId",
                        x => x.TelevisionEpisodesId,
                        "TelevisionEpisodes",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionMovies",
                table => new
                {
                    MoviesId = table.Column<int>("INTEGER", nullable: false),
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionMovies",
                        x => new { x.MoviesId, x.SimpleMediaCollectionsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionMovies_Movies_MoviesId",
                        x => x.MoviesId,
                        "Movies",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionMovies_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionSeasons",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionSeasonsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionSeasons",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionSeasonsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionSeasons_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionSeasons_TelevisionSeasons_TelevisionSeasonsId",
                        x => x.TelevisionSeasonsId,
                        "TelevisionSeasons",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionShows",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionShowsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionShows",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionShowsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionShows_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionShows_TelevisionShows_TelevisionShowsId",
                        x => x.TelevisionShowsId,
                        "TelevisionShows",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionEpisodes_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisodes",
                "TelevisionEpisodesId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionMovies_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovies",
                "SimpleMediaCollectionsId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionSeasons_TelevisionSeasonsId",
                "SimpleMediaCollectionSeasons",
                "TelevisionSeasonsId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionShows_TelevisionShowsId",
                "SimpleMediaCollectionShows",
                "TelevisionShowsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "SimpleMediaCollectionEpisodes");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionMovies");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionSeasons");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionShows");

            migrationBuilder.CreateTable(
                "MediaItemSimpleMediaCollection",
                table => new
                {
                    ItemsId = table.Column<int>("INTEGER", nullable: false),
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_MediaItemSimpleMediaCollection",
                        x => new { x.ItemsId, x.SimpleMediaCollectionsId });
                    table.ForeignKey(
                        "FK_MediaItemSimpleMediaCollection_MediaItems_ItemsId",
                        x => x.ItemsId,
                        "MediaItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_MediaItemSimpleMediaCollection_SimpleMediaCollections_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionMediaCollections",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeasonNumber = table.Column<int>("INTEGER", nullable: true),
                    ShowTitle = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionMediaCollections", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionMediaCollections_MediaCollections_Id",
                        x => x.Id,
                        "MediaCollections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_MediaItemSimpleMediaCollection_SimpleMediaCollectionsId",
                "MediaItemSimpleMediaCollection",
                "SimpleMediaCollectionsId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionMediaCollections_ShowTitle_SeasonNumber",
                "TelevisionMediaCollections",
                new[] { "ShowTitle", "SeasonNumber" },
                unique: true);
        }
    }
}
