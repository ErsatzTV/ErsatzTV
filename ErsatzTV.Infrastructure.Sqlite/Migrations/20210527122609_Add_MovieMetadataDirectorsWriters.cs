using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MovieMetadataDirectorsWriters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // local and plex
            migrationBuilder.Sql("UPDATE MovieMetadata SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql(
                @"UPDATE LibraryFolder SET Etag = NULL WHERE LibraryPathId IN
                    (SELECT LibraryPathId FROM LibraryPath LP
                    INNER JOIN Library L on LP.LibraryId = L.Id
                    WHERE L.MediaKind = 1)");

            // emby
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");

            // jellyfin
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");

            migrationBuilder.CreateTable(
                "Director",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    MovieMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Director", x => x.Id);
                    table.ForeignKey(
                        "FK_Director_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Writer",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>("TEXT", nullable: true),
                    MovieMetadataId = table.Column<int>("INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Writer", x => x.Id);
                    table.ForeignKey(
                        "FK_Writer_MovieMetadata_MovieMetadataId",
                        x => x.MovieMetadataId,
                        "MovieMetadata",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Director_MovieMetadataId",
                "Director",
                "MovieMetadataId");

            migrationBuilder.CreateIndex(
                "IX_Writer_MovieMetadataId",
                "Writer",
                "MovieMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Director");

            migrationBuilder.DropTable(
                "Writer");
        }
    }
}
