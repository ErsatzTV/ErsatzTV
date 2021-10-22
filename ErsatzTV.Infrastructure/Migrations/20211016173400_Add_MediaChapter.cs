using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MediaChapter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaChapter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChapterId = table.Column<long>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaChapter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaChapter_MediaVersion_MediaVersionId",
                        column: x => x.MediaVersionId,
                        principalTable: "MediaVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaChapter_MediaVersionId",
                table: "MediaChapter",
                column: "MediaVersionId");
            
            migrationBuilder.Sql("UPDATE MediaVersion SET DateUpdated = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE LibraryFolder SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbySeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE EmbyEpisode SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinMovie SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinShow SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinSeason SET Etag = NULL");
            migrationBuilder.Sql("UPDATE JellyfinEpisode SET Etag = NULL");
            migrationBuilder.Sql("UPDATE LibraryPath SET LastScan = '0001-01-01 00:00:00'");
            migrationBuilder.Sql("UPDATE Library SET LastScan = '0001-01-01 00:00:00'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaChapter");
        }
    }
}
