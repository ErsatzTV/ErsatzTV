using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MetadataGenres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Genre",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    EpisodeMetadataId = table.Column<int>("INTEGER", nullable: true),
                    MovieMetadataId = table.Column<int>("INTEGER", nullable: true),
                    SeasonMetadataId = table.Column<int>("INTEGER", nullable: true),
                    ShowMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.Id);
                    table.ForeignKey(
                        "FK_Genre_EpisodeMetadata_EpisodeMetadataId",
                        x => x.EpisodeMetadataId,
                        "EpisodeMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Genre_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Genre_SeasonMetadata_SeasonMetadataId",
                        x => x.SeasonMetadataId,
                        "SeasonMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Genre_ShowMetadata_ShowMetadataId",
                        x => x.ShowMetadataId,
                        "ShowMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Genre_EpisodeMetadataId",
                "Genre",
                "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Genre_MovieMetadataId",
                "Genre",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Genre_SeasonMetadataId",
                "Genre",
                "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Genre_ShowMetadataId",
                "Genre",
                "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "Genre");
    }
}
