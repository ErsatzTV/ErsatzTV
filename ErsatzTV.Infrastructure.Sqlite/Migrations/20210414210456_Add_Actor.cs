using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_Actor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Actor",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    Role = table.Column<string>("TEXT", nullable: true),
                    Order = table.Column<int>("INTEGER", nullable: true),
                    ArtistMetadataId = table.Column<int>("INTEGER", nullable: true),
                    EpisodeMetadataId = table.Column<int>("INTEGER", nullable: true),
                    MovieMetadataId = table.Column<int>("INTEGER", nullable: true),
                    MusicVideoMetadataId = table.Column<int>("INTEGER", nullable: true),
                    SeasonMetadataId = table.Column<int>("INTEGER", nullable: true),
                    ShowMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actor", x => x.Id);
                    table.ForeignKey(
                        "FK_Actor_ArtistMetadata_ArtistMetadataId",
                        x => x.ArtistMetadataId,
                        "ArtistMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Actor_EpisodeMetadata_EpisodeMetadataId",
                        x => x.EpisodeMetadataId,
                        "EpisodeMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Actor_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Actor_MusicVideoMetadata_MusicVideoMetadataId",
                        x => x.MusicVideoMetadataId,
                        "MusicVideoMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Actor_SeasonMetadata_SeasonMetadataId",
                        x => x.SeasonMetadataId,
                        "SeasonMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Actor_ShowMetadata_ShowMetadataId",
                        x => x.ShowMetadataId,
                        "ShowMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Actor_ArtistMetadataId",
                "Actor",
                "ArtistMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Actor_EpisodeMetadataId",
                "Actor",
                "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Actor_MovieMetadataId",
                "Actor",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Actor_MusicVideoMetadataId",
                "Actor",
                "MusicVideoMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Actor_SeasonMetadataId",
                "Actor",
                "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Actor_ShowMetadataId",
                "Actor",
                "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "Actor");
    }
}
