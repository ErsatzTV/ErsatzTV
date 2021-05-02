using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_JellyfinMediaSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JellyfinMediaSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinMediaSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinMediaSource_MediaSource_Id",
                        column: x => x.Id,
                        principalTable: "MediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JellyfinConnection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    JellyfinMediaSourceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinConnection_JellyfinMediaSource_JellyfinMediaSourceId",
                        column: x => x.JellyfinMediaSourceId,
                        principalTable: "JellyfinMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JellyfinPathReplacement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JellyfinPath = table.Column<string>(type: "TEXT", nullable: true),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    JellyfinMediaSourceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinPathReplacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinPathReplacement_JellyfinMediaSource_JellyfinMediaSourceId",
                        column: x => x.JellyfinMediaSourceId,
                        principalTable: "JellyfinMediaSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinConnection_JellyfinMediaSourceId",
                table: "JellyfinConnection",
                column: "JellyfinMediaSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinPathReplacement_JellyfinMediaSourceId",
                table: "JellyfinPathReplacement",
                column: "JellyfinMediaSourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JellyfinConnection");

            migrationBuilder.DropTable(
                name: "JellyfinPathReplacement");

            migrationBuilder.DropTable(
                name: "JellyfinMediaSource");
        }
    }
}
