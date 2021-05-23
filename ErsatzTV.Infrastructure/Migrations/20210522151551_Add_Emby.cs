using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_Emby : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "EmbyEpisode",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyEpisode", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyEpisode_Episode_Id",
                        x => x.Id,
                        "Episode",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyLibrary",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    ShouldSyncItems = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyLibrary", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyLibrary_Library_Id",
                        x => x.Id,
                        "Library",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyMediaSource",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>("TEXT", nullable: true),
                    OperatingSystem = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyMediaSource", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyMediaSource_MediaSource_Id",
                        x => x.Id,
                        "MediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyMovie",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyMovie", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyMovie_Movie_Id",
                        x => x.Id,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbySeason",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbySeason", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbySeason_Season_Id",
                        x => x.Id,
                        "Season",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyShow",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyShow", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyShow_Show_Id",
                        x => x.Id,
                        "Show",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyConnection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>("TEXT", nullable: true),
                    EmbyMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyConnection", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyConnection_EmbyMediaSource_EmbyMediaSourceId",
                        x => x.EmbyMediaSourceId,
                        "EmbyMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "EmbyPathReplacement",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmbyPath = table.Column<string>("TEXT", nullable: true),
                    LocalPath = table.Column<string>("TEXT", nullable: true),
                    EmbyMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyPathReplacement", x => x.Id);
                    table.ForeignKey(
                        "FK_EmbyPathReplacement_EmbyMediaSource_EmbyMediaSourceId",
                        x => x.EmbyMediaSourceId,
                        "EmbyMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_EmbyConnection_EmbyMediaSourceId",
                "EmbyConnection",
                "EmbyMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_EmbyPathReplacement_EmbyMediaSourceId",
                "EmbyPathReplacement",
                "EmbyMediaSourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "EmbyConnection");

            migrationBuilder.DropTable(
                "EmbyEpisode");

            migrationBuilder.DropTable(
                "EmbyLibrary");

            migrationBuilder.DropTable(
                "EmbyMovie");

            migrationBuilder.DropTable(
                "EmbyPathReplacement");

            migrationBuilder.DropTable(
                "EmbySeason");

            migrationBuilder.DropTable(
                "EmbyShow");

            migrationBuilder.DropTable(
                "EmbyMediaSource");
        }
    }
}
