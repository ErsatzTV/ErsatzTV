using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MetadataGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // local and plex
            migrationBuilder.Sql("UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE ShowMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE SeasonMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE EpisodeMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE LibraryPathId IN
                    (SELECT LibraryPathId FROM LibraryPath LP
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    WHERE L.MediaKind = 1)");

            // emby
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbySeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyEpisode SET Etag = NULL");

            // jellyfin
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinEpisode SET Etag = NULL");

            migrationBuilder.CreateTable(
                name: "MetadataGuid",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Guid = table.Column<string>(type: "TEXT", nullable: true),
                    ArtistMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataGuid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetadataGuid_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_ArtistMetadataId",
                table: "MetadataGuid",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_EpisodeMetadataId",
                table: "MetadataGuid",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_MovieMetadataId",
                table: "MetadataGuid",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_MusicVideoMetadataId",
                table: "MetadataGuid",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_SeasonMetadataId",
                table: "MetadataGuid",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataGuid_ShowMetadataId",
                table: "MetadataGuid",
                column: "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataGuid");
        }
    }
}
