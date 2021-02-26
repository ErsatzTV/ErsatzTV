using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_VersionsAndMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                "SeasonNumber",
                "Season",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "EpisodeNumber",
                "Episode",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                "EpisodeMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Tagline = table.Column<string>("TEXT", nullable: true),
                    EpisodeId = table.Column<int>("INTEGER", nullable: false),
                    MetadataKind = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    OriginalTitle = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    ReleaseDate = table.Column<DateTimeOffset>("TEXT", nullable: true),
                    DateAdded = table.Column<DateTimeOffset>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>("TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_EpisodeMetadata_Episode_EpisodeId",
                        x => x.EpisodeId,
                        "Episode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "MediaVersion",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>("TEXT", nullable: false),
                    SampleAspectRatio = table.Column<string>("TEXT", nullable: true),
                    DisplayAspectRatio = table.Column<string>("TEXT", nullable: true),
                    VideoCodec = table.Column<string>("TEXT", nullable: true),
                    AudioCodec = table.Column<string>("TEXT", nullable: true),
                    IsInterlaced = table.Column<bool>("INTEGER", nullable: false),
                    Width = table.Column<int>("INTEGER", nullable: false),
                    Height = table.Column<int>("INTEGER", nullable: false),
                    EpisodeId = table.Column<int>("INTEGER", nullable: true),
                    MovieId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVersion", x => x.Id);
                    table.ForeignKey(
                        "FK_MediaVersion_Episode_EpisodeId",
                        x => x.EpisodeId,
                        "Episode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_MediaVersion_Movie_MovieId",
                        x => x.MovieId,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "NewMovieMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Tagline = table.Column<string>("TEXT", nullable: true),
                    MovieId = table.Column<int>("INTEGER", nullable: false),
                    MetadataKind = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    OriginalTitle = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    ReleaseDate = table.Column<DateTimeOffset>("TEXT", nullable: true),
                    DateAdded = table.Column<DateTimeOffset>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>("TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewMovieMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_NewMovieMetadata_Movie_MovieId",
                        x => x.MovieId,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ShowMetadata",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Outline = table.Column<string>("TEXT", nullable: true),
                    Plot = table.Column<string>("TEXT", nullable: true),
                    Tagline = table.Column<string>("TEXT", nullable: true),
                    ShowId = table.Column<int>("INTEGER", nullable: false),
                    MetadataKind = table.Column<int>("INTEGER", nullable: false),
                    Title = table.Column<string>("TEXT", nullable: true),
                    OriginalTitle = table.Column<string>("TEXT", nullable: true),
                    SortTitle = table.Column<string>("TEXT", nullable: true),
                    ReleaseDate = table.Column<DateTimeOffset>("TEXT", nullable: true),
                    DateAdded = table.Column<DateTimeOffset>("TEXT", nullable: false),
                    DateUpdated = table.Column<DateTimeOffset>("TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowMetadata", x => x.Id);
                    table.ForeignKey(
                        "FK_ShowMetadata_Show_ShowId",
                        x => x.ShowId,
                        "Show",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "MediaFile",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>("TEXT", nullable: true),
                    MediaVersionId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFile", x => x.Id);
                    table.ForeignKey(
                        "FK_MediaFile_MediaVersion_MediaVersionId",
                        x => x.MediaVersionId,
                        "MediaVersion",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_EpisodeMetadata_EpisodeId",
                "EpisodeMetadata",
                "EpisodeId");

            migrationBuilder.CreateIndex(
                "IX_MediaFile_MediaVersionId",
                "MediaFile",
                "MediaVersionId");

            migrationBuilder.CreateIndex(
                "IX_MediaVersion_EpisodeId",
                "MediaVersion",
                "EpisodeId");

            migrationBuilder.CreateIndex(
                "IX_MediaVersion_MovieId",
                "MediaVersion",
                "MovieId");

            migrationBuilder.CreateIndex(
                "IX_NewMovieMetadata_MovieId",
                "NewMovieMetadata",
                "MovieId");

            migrationBuilder.CreateIndex(
                "IX_ShowMetadata_ShowId",
                "ShowMetadata",
                "ShowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "EpisodeMetadata");

            migrationBuilder.DropTable(
                "MediaFile");

            migrationBuilder.DropTable(
                "NewMovieMetadata");

            migrationBuilder.DropTable(
                "ShowMetadata");

            migrationBuilder.DropTable(
                "MediaVersion");

            migrationBuilder.DropColumn(
                "SeasonNumber",
                "Season");

            migrationBuilder.DropColumn(
                "EpisodeNumber",
                "Episode");
        }
    }
}
