using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class TelevisionMediaItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "TelevisionEpisodeId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "TelevisionSeasonId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "TelevisionShowId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                "Collection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Collection", x => x.Id); });

            migrationBuilder.CreateTable(
                "Show",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Show", x => x.Id);
                    table.ForeignKey(
                        "FK_Show_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "CollectionItem",
                table => new
                {
                    CollectionId = table.Column<int>("INTEGER", nullable: false),
                    MediaItemId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionItem", x => new { x.CollectionId, x.MediaItemId });
                    table.ForeignKey(
                        "FK_CollectionItem_Collection_CollectionId",
                        x => x.CollectionId,
                        "Collection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CollectionItem_MediaItem_MediaItemId",
                        x => x.MediaItemId,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Season",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Season", x => x.Id);
                    table.ForeignKey(
                        "FK_Season_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Season_Show_ShowId",
                        x => x.ShowId,
                        "Show",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Episode",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeasonId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episode", x => x.Id);
                    table.ForeignKey(
                        "FK_Episode_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Episode_Season_SeasonId",
                        x => x.SeasonId,
                        "Season",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_CollectionItem_MediaItemId",
                "CollectionItem",
                "MediaItemId");

            migrationBuilder.CreateIndex(
                "IX_Episode_SeasonId",
                "Episode",
                "SeasonId");

            migrationBuilder.CreateIndex(
                "IX_Season_ShowId",
                "Season",
                "ShowId");

            // create show media items
            migrationBuilder.Sql(
                @"INSERT INTO MediaItem (TelevisionShowId, LibraryPathId)
                    SELECT distinct ts.Id, mi.LibraryPathId
                    FROM TelevisionShow ts
                    INNER JOIN TelevisionSeason tsn ON tsn.TelevisionShowId = ts.Id
                    INNER JOIN TelevisionEpisode te ON te.SeasonId = tsn.Id
                    INNER JOIN MediaItem mi on mi.Id = te.Id");

            // create shows
            migrationBuilder.Sql(@"INSERT INTO Show (Id) SELECT Id FROM MediaItem WHERE TelevisionShowId > 0");

            // create season media items
            migrationBuilder.Sql(
                @"INSERT INTO MediaItem (TelevisionSeasonId, LibraryPathId)
                    SELECT distinct tsn.Id, mi.LibraryPathId
                    FROM TelevisionSeason tsn
                    INNER JOIN TelevisionEpisode te ON te.SeasonId = tsn.Id
                    INNER JOIN MediaItem mi on mi.Id = te.Id");

            // create seasons
            migrationBuilder.Sql(
                @"INSERT INTO Season (Id, ShowId)
                    SELECT mi.Id, mi2.Id
                    FROM MediaItem mi
                    INNER JOIN TelevisionSeason tsn ON tsn.Id = mi.TelevisionSeasonId
                    INNER JOIN MediaItem mi2 ON mi2.TelevisionShowId = tsn.TelevisionShowId AND mi.LibraryPathId = mi2.LibraryPathId");

            // mark episode media items
            migrationBuilder.Sql(
                @"UPDATE MediaItem SET TelevisionEpisodeId = Id
                    WHERE Id IN (SELECT Id FROM TelevisionEpisode)");

            // create episodes
            migrationBuilder.Sql(
                @"INSERT INTO Episode (Id, SeasonId)
                    SELECT mi.Id, mi2.Id
                    FROM MediaItem mi
                    INNER JOIN TelevisionEpisode te on mi.Id = te.Id
                    INNER JOIN MediaItem mi2 ON mi2.TelevisionSeasonId = te.SeasonId AND mi.LibraryPathId = mi2.LibraryPathId");

            // collections
            migrationBuilder.Sql(@"INSERT INTO Collection (Name) SELECT Name FROM MediaCollection");

            // collection movies
            migrationBuilder.Sql(
                @"INSERT INTO CollectionItem (CollectionId, MediaItemId)
            SELECT c.Id, smcm.MoviesId
            FROM Collection c
            INNER JOIN MediaCollection mc on mc.Name = c.Name
            INNER JOIN SimpleMediaCollectionMovie smcm ON smcm.SimpleMediaCollectionsId = mc.Id");

            // collection shows
            migrationBuilder.Sql(
                @"INSERT INTO CollectionItem (CollectionId, MediaItemId)
            SELECT c.Id, mi.Id
            FROM Collection c
            INNER JOIN MediaCollection mc on mc.Name = c.Name
            INNER JOIN SimpleMediaCollectionShow smcs ON smcs.SimpleMediaCollectionsId = mc.Id
            INNER JOIN MediaItem mi ON mi.TelevisionShowId = smcs.TelevisionShowsId");

            // collection seasons
            migrationBuilder.Sql(
                @"INSERT INTO CollectionItem (CollectionId, MediaItemId)
            SELECT c.Id, mi.Id
            FROM Collection c
            INNER JOIN MediaCollection mc on mc.Name = c.Name
            INNER JOIN SimpleMediaCollectionSeason smcs ON smcs.SimpleMediaCollectionsId = mc.Id
            INNER JOIN MediaItem mi ON mi.TelevisionSeasonId = smcs.TelevisionSeasonsId");

            // collection episodes
            migrationBuilder.Sql(
                @"INSERT INTO CollectionItem (CollectionId, MediaItemId)
            SELECT c.Id, mi.Id
            FROM Collection c
            INNER JOIN MediaCollection mc on mc.Name = c.Name
            INNER JOIN SimpleMediaCollectionEpisode smce ON smce.SimpleMediaCollectionsId = mc.Id
            INNER JOIN MediaItem mi ON mi.TelevisionEpisodeId = smce.TelevisionEpisodesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "CollectionItem");

            migrationBuilder.DropTable(
                "Episode");

            migrationBuilder.DropTable(
                "Collection");

            migrationBuilder.DropTable(
                "Season");

            migrationBuilder.DropTable(
                "Show");

            migrationBuilder.DropColumn(
                "TelevisionEpisodeId",
                "MediaItem");

            migrationBuilder.DropColumn(
                "TelevisionSeasonId",
                "MediaItem");

            migrationBuilder.DropColumn(
                "TelevisionShowId",
                "MediaItem");
        }
    }
}
