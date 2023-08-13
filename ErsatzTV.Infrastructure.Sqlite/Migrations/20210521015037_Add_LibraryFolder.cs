using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_LibraryFolder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "LibraryFolder",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>("TEXT", nullable: true),
                    LibraryPathId = table.Column<int>("INTEGER", nullable: false),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryFolder", x => x.Id);
                    table.ForeignKey(
                        "FK_LibraryFolder_LibraryPath_LibraryPathId",
                        x => x.LibraryPathId,
                        "LibraryPath",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_LibraryFolder_LibraryPathId",
                "LibraryFolder",
                "LibraryPathId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "LibraryFolder");
    }
}
