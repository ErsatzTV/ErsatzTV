using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_JellyfinLibraryPathInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JellyfinPathInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    NetworkPath = table.Column<string>(type: "TEXT", nullable: true),
                    JellyfinLibraryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinPathInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JellyfinPathInfo_JellyfinLibrary_JellyfinLibraryId",
                        column: x => x.JellyfinLibraryId,
                        principalTable: "JellyfinLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JellyfinPathInfo_JellyfinLibraryId",
                table: "JellyfinPathInfo",
                column: "JellyfinLibraryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JellyfinPathInfo");
        }
    }
}
