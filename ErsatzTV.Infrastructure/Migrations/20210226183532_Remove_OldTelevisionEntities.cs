using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_OldTelevisionEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ProgramScheduleItem SET
                               MediaCollectionId = null,
                               TelevisionSeasonId = null,
                               TelevisionShowId = null");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_MediaCollection_MediaCollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_TelevisionSeason_TelevisionSeasonId",
                "ProgramScheduleItem");

            migrationBuilder.DropForeignKey(
                "FK_ProgramScheduleItem_TelevisionShow_TelevisionShowId",
                "ProgramScheduleItem");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionEpisode");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionMovie");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionSeason");

            migrationBuilder.DropTable(
                "SimpleMediaCollectionShow");

            migrationBuilder.DropTable(
                "TelevisionEpisodeMetadata");

            migrationBuilder.DropTable(
                "TelevisionShowMetadata");

            migrationBuilder.DropTable(
                "TelevisionShowSource");

            migrationBuilder.DropTable(
                "SimpleMediaCollection");

            migrationBuilder.DropTable(
                "TelevisionEpisode");

            migrationBuilder.DropTable(
                "MediaCollection");

            migrationBuilder.DropTable(
                "TelevisionSeason");

            migrationBuilder.DropTable(
                "TelevisionShow");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItem_MediaCollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItem_TelevisionSeasonId",
                "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                "IX_ProgramScheduleItem_TelevisionShowId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "MediaCollectionId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "TelevisionSeasonId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "TelevisionShowId",
                "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                "CollectionId",
                "PlayoutProgramScheduleAnchor");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "MediaCollectionId",
                "ProgramScheduleItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "TelevisionSeasonId",
                "ProgramScheduleItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "TelevisionShowId",
                "ProgramScheduleItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "CollectionId",
                "PlayoutProgramScheduleAnchor",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                "MediaCollection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_MediaCollection", x => x.Id); });

            migrationBuilder.CreateTable(
                "TelevisionShow",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Poster = table.Column<string>("TEXT", nullable: true),
                    PosterLastWriteTime = table.Column<DateTime>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_TelevisionShow", x => x.Id); });

            migrationBuilder.CreateTable(
                "SimpleMediaCollection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleMediaCollection", x => x.Id);
                    table.ForeignKey(
                        "FK_SimpleMediaCollection_MediaCollection_Id",
                        x => x.Id,
                        "MediaCollection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionSeason",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>("INTEGER", nullable: false),
                    Path = table.Column<string>("TEXT", nullable: true),
                    Poster = table.Column<string>("TEXT", nullable: true),
                    PosterLastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionSeason", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionSeason_TelevisionShow_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShow",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionShowMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    Year = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionShowMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionShowMetadata_TelevisionShow_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShow",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionShowSource",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Discriminator = table.Column<string>("TEXT", nullable: false),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false),
                    MediaSourceId = table.Column<int>("INTEGER", nullable: true),
                    Path = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionShowSource", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionShowSource_LocalMediaSource_MediaSourceId",
                        x => x.MediaSourceId,
                        "LocalMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TelevisionShowSource_TelevisionShow_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShow",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionMovie",
                table => new
                {
                    MoviesId = table.Column<int>("INTEGER", nullable: false),
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionMovie",
                        x => new { x.MoviesId, x.SimpleMediaCollectionsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionMovie_Movie_MoviesId",
                        x => x.MoviesId,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionMovie_SimpleMediaCollection_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionShow",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionShowsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionShow",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionShowsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionShow_SimpleMediaCollection_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionShow_TelevisionShow_TelevisionShowsId",
                        x => x.TelevisionShowsId,
                        "TelevisionShow",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionSeason",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionSeasonsId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionSeason",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionSeasonsId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionSeason_SimpleMediaCollection_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionSeason_TelevisionSeason_TelevisionSeasonsId",
                        x => x.TelevisionSeasonsId,
                        "TelevisionSeason",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionEpisode",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeasonId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionEpisode", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionEpisode_MediaItem_Id",
                        x => x.Id,
                        "MediaItem",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TelevisionEpisode_TelevisionSeason_SeasonId",
                        x => x.SeasonId,
                        "TelevisionSeason",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "SimpleMediaCollectionEpisode",
                table => new
                {
                    SimpleMediaCollectionsId = table.Column<int>("INTEGER", nullable: false),
                    TelevisionEpisodesId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SimpleMediaCollectionEpisode",
                        x => new { x.SimpleMediaCollectionsId, x.TelevisionEpisodesId });
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionEpisode_SimpleMediaCollection_SimpleMediaCollectionsId",
                        x => x.SimpleMediaCollectionsId,
                        "SimpleMediaCollection",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_SimpleMediaCollectionEpisode_TelevisionEpisode_TelevisionEpisodesId",
                        x => x.TelevisionEpisodesId,
                        "TelevisionEpisode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionEpisodeMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Aired = table.Column<DateTime>("TEXT", nullable: true),
                    Episode = table.Column<int>("INTEGER", nullable: false),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Season = table.Column<int>("INTEGER", nullable: false),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    TelevisionEpisodeId = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionEpisodeMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionEpisodeMetadata_TelevisionEpisode_TelevisionEpisodeId",
                        x => x.TelevisionEpisodeId,
                        "TelevisionEpisode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItem_MediaCollectionId",
                "ProgramScheduleItem",
                "MediaCollectionId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItem_TelevisionSeasonId",
                "ProgramScheduleItem",
                "TelevisionSeasonId");

            migrationBuilder.CreateIndex(
                "IX_ProgramScheduleItem_TelevisionShowId",
                "ProgramScheduleItem",
                "TelevisionShowId");

            migrationBuilder.CreateIndex(
                "IX_MediaCollection_Name",
                "MediaCollection",
                "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionEpisode_TelevisionEpisodesId",
                "SimpleMediaCollectionEpisode",
                "TelevisionEpisodesId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionMovie_SimpleMediaCollectionsId",
                "SimpleMediaCollectionMovie",
                "SimpleMediaCollectionsId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionSeason_TelevisionSeasonsId",
                "SimpleMediaCollectionSeason",
                "TelevisionSeasonsId");

            migrationBuilder.CreateIndex(
                "IX_SimpleMediaCollectionShow_TelevisionShowsId",
                "SimpleMediaCollectionShow",
                "TelevisionShowsId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionEpisode_SeasonId",
                "TelevisionEpisode",
                "SeasonId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionEpisodeMetadata_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata",
                "TelevisionEpisodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TelevisionSeason_TelevisionShowId",
                "TelevisionSeason",
                "TelevisionShowId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionShowMetadata_TelevisionShowId",
                "TelevisionShowMetadata",
                "TelevisionShowId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TelevisionShowSource_MediaSourceId",
                "TelevisionShowSource",
                "MediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionShowSource_TelevisionShowId",
                "TelevisionShowSource",
                "TelevisionShowId");

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_MediaCollection_MediaCollectionId",
                "ProgramScheduleItem",
                "MediaCollectionId",
                "MediaCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_TelevisionSeason_TelevisionSeasonId",
                "ProgramScheduleItem",
                "TelevisionSeasonId",
                "TelevisionSeason",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_ProgramScheduleItem_TelevisionShow_TelevisionShowId",
                "ProgramScheduleItem",
                "TelevisionShowId",
                "TelevisionShow",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
