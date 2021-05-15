using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_JellyfinMediaSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "JellyfinMediaSource",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinMediaSource", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinMediaSource_MediaSource_Id",
                        x => x.Id,
                        "MediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "JellyfinConnection",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>("TEXT", nullable: true),
                    JellyfinMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinConnection", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinConnection_JellyfinMediaSource_JellyfinMediaSourceId",
                        x => x.JellyfinMediaSourceId,
                        "JellyfinMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "JellyfinPathReplacement",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JellyfinPath = table.Column<string>("TEXT", nullable: true),
                    LocalPath = table.Column<string>("TEXT", nullable: true),
                    JellyfinMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinPathReplacement", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinPathReplacement_JellyfinMediaSource_JellyfinMediaSourceId",
                        x => x.JellyfinMediaSourceId,
                        "JellyfinMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_JellyfinConnection_JellyfinMediaSourceId",
                "JellyfinConnection",
                "JellyfinMediaSourceId");

            migrationBuilder.CreateIndex(
                "IX_JellyfinPathReplacement_JellyfinMediaSourceId",
                "JellyfinPathReplacement",
                "JellyfinMediaSourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "JellyfinConnection");

            migrationBuilder.DropTable(
                "JellyfinPathReplacement");

            migrationBuilder.DropTable(
                "JellyfinMediaSource");
        }
    }
}
