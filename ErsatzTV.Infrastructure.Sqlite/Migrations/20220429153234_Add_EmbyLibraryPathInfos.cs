using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_EmbyLibraryPathInfos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmbyPathInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    NetworkPath = table.Column<string>(type: "TEXT", nullable: true),
                    EmbyLibraryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbyPathInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmbyPathInfo_EmbyLibrary_EmbyLibraryId",
                        column: x => x.EmbyLibraryId,
                        principalTable: "EmbyLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmbyPathInfo_EmbyLibraryId",
                table: "EmbyPathInfo",
                column: "EmbyLibraryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmbyPathInfo");
        }
    }
}
