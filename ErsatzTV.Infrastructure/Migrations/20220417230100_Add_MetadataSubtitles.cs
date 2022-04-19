using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MetadataSubtitles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subtitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    SubtitleKind = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ArtistMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    EpisodeMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    MovieMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    OtherVideoMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeasonMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShowMetadataId = table.Column<int>(type: "INTEGER", nullable: true),
                    SongMetadataId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtitle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtitle_ArtistMetadata_ArtistMetadataId",
                        column: x => x.ArtistMetadataId,
                        principalTable: "ArtistMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subtitle_EpisodeMetadata_EpisodeMetadataId",
                        column: x => x.EpisodeMetadataId,
                        principalTable: "EpisodeMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_MovieMetadata_MovieMetadataId",
                        column: x => x.MovieMetadataId,
                        principalTable: "MovieMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_OtherVideoMetadata_OtherVideoMetadataId",
                        column: x => x.OtherVideoMetadataId,
                        principalTable: "OtherVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtitle_SeasonMetadata_SeasonMetadataId",
                        column: x => x.SeasonMetadataId,
                        principalTable: "SeasonMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subtitle_ShowMetadata_ShowMetadataId",
                        column: x => x.ShowMetadataId,
                        principalTable: "ShowMetadata",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subtitle_SongMetadata_SongMetadataId",
                        column: x => x.SongMetadataId,
                        principalTable: "SongMetadata",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_ArtistMetadataId",
                table: "Subtitle",
                column: "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_EpisodeMetadataId",
                table: "Subtitle",
                column: "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_MovieMetadataId",
                table: "Subtitle",
                column: "MovieMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_MusicVideoMetadataId",
                table: "Subtitle",
                column: "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_OtherVideoMetadataId",
                table: "Subtitle",
                column: "OtherVideoMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_SeasonMetadataId",
                table: "Subtitle",
                column: "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_ShowMetadataId",
                table: "Subtitle",
                column: "ShowMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtitle_SongMetadataId",
                table: "Subtitle",
                column: "SongMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subtitle");
        }
    }
}
