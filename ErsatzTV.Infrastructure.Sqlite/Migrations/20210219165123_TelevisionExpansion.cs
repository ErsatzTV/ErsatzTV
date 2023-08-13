using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class TelevisionExpansion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Metadata_Aired",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_ContentRating",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_Description",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_EpisodeNumber",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_MediaType",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_SeasonNumber",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_SortTitle",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_Source",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_Subtitle",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_Title",
                "MediaItems");

            migrationBuilder.RenameColumn(
                "Metadata_Width",
                "MediaItems",
                "Statistics_Width");

            migrationBuilder.RenameColumn(
                "Metadata_VideoScanType",
                "MediaItems",
                "Statistics_VideoScanType");

            migrationBuilder.RenameColumn(
                "Metadata_VideoCodec",
                "MediaItems",
                "Statistics_VideoCodec");

            migrationBuilder.RenameColumn(
                "Metadata_SampleAspectRatio",
                "MediaItems",
                "Statistics_SampleAspectRatio");

            migrationBuilder.RenameColumn(
                "Metadata_LastWriteTime",
                "MediaItems",
                "Statistics_LastWriteTime");

            migrationBuilder.RenameColumn(
                "Metadata_Height",
                "MediaItems",
                "Statistics_Height");

            migrationBuilder.RenameColumn(
                "Metadata_Duration",
                "MediaItems",
                "Statistics_Duration");

            migrationBuilder.RenameColumn(
                "Metadata_DisplayAspectRatio",
                "MediaItems",
                "Statistics_DisplayAspectRatio");

            migrationBuilder.RenameColumn(
                "Metadata_AudioCodec",
                "MediaItems",
                "Statistics_AudioCodec");

            migrationBuilder.CreateTable(
                "Movies",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MetadataId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                    table.ForeignKey(
                        "FK_Movies_MediaItems_Id",
                        x => x.Id,
                        "MediaItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionShows",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Poster = table.Column<string>("TEXT", nullable: true),
                    PosterLastWriteTime = table.Column<DateTime>("TEXT", nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_TelevisionShows", x => x.Id); });

            migrationBuilder.CreateTable(
                "MovieMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MovieId = table.Column<int>("INTEGER", nullable: false),
                    Year = table.Column<int>("INTEGER", nullable: true),
                    Premiered = table.Column<DateTime>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    Tagline = table.Column<string>("TEXT", nullable: true),
                    ContentRating = table.Column<string>("TEXT", nullable: true),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    Title = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_MovieMetadata_Movies_MovieId",
                        x => x.MovieId,
                        "Movies",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionSeasons",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false),
                    Number = table.Column<int>("INTEGER", nullable: false),
                    Path = table.Column<string>("TEXT", nullable: true),
                    Poster = table.Column<string>("TEXT", nullable: true),
                    PosterLastWriteTime = table.Column<DateTime>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionSeasons", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionSeasons_TelevisionShows_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShows",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionShowMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    Title = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    Year = table.Column<int>("INTEGER", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionShowMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionShowMetadata_TelevisionShows_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShows",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionShowSource",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelevisionShowId = table.Column<int>("INTEGER", nullable: false),
                    Discriminator = table.Column<string>("TEXT", nullable: false),
                    MediaSourceId = table.Column<int>("INTEGER", nullable: true),
                    Path = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionShowSource", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionShowSource_LocalMediaSources_MediaSourceId",
                        x => x.MediaSourceId,
                        "LocalMediaSources",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TelevisionShowSource_TelevisionShows_TelevisionShowId",
                        x => x.TelevisionShowId,
                        "TelevisionShows",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionEpisodes",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeasonId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionEpisodes", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionEpisodes_MediaItems_Id",
                        x => x.Id,
                        "MediaItems",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TelevisionEpisodes_TelevisionSeasons_SeasonId",
                        x => x.SeasonId,
                        "TelevisionSeasons",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TelevisionEpisodeMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TelevisionEpisodeId = table.Column<int>("INTEGER", nullable: false),
                    Season = table.Column<int>("INTEGER", nullable: false),
                    Episode = table.Column<int>("INTEGER", nullable: false),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Aired = table.Column<DateTime>("TEXT", nullable: true),
                    Source = table.Column<int>("INTEGER", nullable: false),
                    LastWriteTime = table.Column<DateTime>("TEXT", nullable: true),
                    Title = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelevisionEpisodeMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_TelevisionEpisodeMetadata_TelevisionEpisodes_TelevisionEpisodeId",
                        x => x.TelevisionEpisodeId,
                        "TelevisionEpisodes",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_MovieMetadata_MovieId",
                "MovieMetadata",
                "MovieId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TelevisionEpisodeMetadata_TelevisionEpisodeId",
                "TelevisionEpisodeMetadata",
                "TelevisionEpisodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TelevisionEpisodes_SeasonId",
                "TelevisionEpisodes",
                "SeasonId");

            migrationBuilder.CreateIndex(
                "IX_TelevisionSeasons_TelevisionShowId",
                "TelevisionSeasons",
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "MovieMetadata");

            migrationBuilder.DropTable(
                "TelevisionEpisodeMetadata");

            migrationBuilder.DropTable(
                "TelevisionShowMetadata");

            migrationBuilder.DropTable(
                "TelevisionShowSource");

            migrationBuilder.DropTable(
                "Movies");

            migrationBuilder.DropTable(
                "TelevisionEpisodes");

            migrationBuilder.DropTable(
                "TelevisionSeasons");

            migrationBuilder.DropTable(
                "TelevisionShows");

            migrationBuilder.RenameColumn(
                "Statistics_Width",
                "MediaItems",
                "Metadata_Width");

            migrationBuilder.RenameColumn(
                "Statistics_VideoScanType",
                "MediaItems",
                "Metadata_VideoScanType");

            migrationBuilder.RenameColumn(
                "Statistics_VideoCodec",
                "MediaItems",
                "Metadata_VideoCodec");

            migrationBuilder.RenameColumn(
                "Statistics_SampleAspectRatio",
                "MediaItems",
                "Metadata_SampleAspectRatio");

            migrationBuilder.RenameColumn(
                "Statistics_LastWriteTime",
                "MediaItems",
                "Metadata_LastWriteTime");

            migrationBuilder.RenameColumn(
                "Statistics_Height",
                "MediaItems",
                "Metadata_Height");

            migrationBuilder.RenameColumn(
                "Statistics_Duration",
                "MediaItems",
                "Metadata_Duration");

            migrationBuilder.RenameColumn(
                "Statistics_DisplayAspectRatio",
                "MediaItems",
                "Metadata_DisplayAspectRatio");

            migrationBuilder.RenameColumn(
                "Statistics_AudioCodec",
                "MediaItems",
                "Metadata_AudioCodec");

            migrationBuilder.AddColumn<DateTime>(
                "Metadata_Aired",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Metadata_ContentRating",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Metadata_Description",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Metadata_EpisodeNumber",
                "MediaItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Metadata_MediaType",
                "MediaItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Metadata_SeasonNumber",
                "MediaItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Metadata_SortTitle",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Metadata_Source",
                "MediaItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Metadata_Subtitle",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Metadata_Title",
                "MediaItems",
                "TEXT",
                nullable: true);
        }
    }
}
