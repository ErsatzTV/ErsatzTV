using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MetadataTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Tag",
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
                    table.PrimaryKey("PK_Tag", x => x.Id);
                    table.ForeignKey(
                        "FK_Tag_EpisodeMetadata_EpisodeMetadataId",
                        x => x.EpisodeMetadataId,
                        "EpisodeMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Tag_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Tag_SeasonMetadata_SeasonMetadataId",
                        x => x.SeasonMetadataId,
                        "SeasonMetadata",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Tag_ShowMetadata_ShowMetadataId",
                        x => x.ShowMetadataId,
                        "ShowMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Tag_EpisodeMetadataId",
                "Tag",
                "EpisodeMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Tag_MovieMetadataId",
                "Tag",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Tag_SeasonMetadataId",
                "Tag",
                "SeasonMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Tag_ShowMetadataId",
                "Tag",
                "ShowMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "Tag");
    }
}
