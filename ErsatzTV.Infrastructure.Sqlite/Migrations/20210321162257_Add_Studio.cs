using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_Studio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Studio",
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
                    table.PrimaryKey("PK_Studio", x => x.Id);
                    table.ForeignKey(
                        "FK_Studio_EpisodeMetadata_EpisodeMetadataId",
                        x => x.EpisodeMetadataId,
                        "EpisodeMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Studio_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Studio_SeasonMetadata_SeasonMetadataId",
                        x => x.SeasonMetadataId,
                        "SeasonMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Studio_ShowMetadata_ShowMetadataId",
                        x => x.ShowMetadataId,
                        "ShowMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Studio_EpisodeMetadataId",
                "Studio",
                "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Studio_MovieMetadataId",
                "Studio",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Studio_SeasonMetadataId",
                "Studio",
                "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Studio_ShowMetadataId",
                "Studio",
                "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "Studio");
    }
}
