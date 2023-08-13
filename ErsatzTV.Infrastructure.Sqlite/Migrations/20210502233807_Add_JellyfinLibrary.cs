using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_JellyfinLibrary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.CreateTable(
                "JellyfinLibrary",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    ShouldSyncItems = table.Column<bool>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinLibrary", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinLibrary_Library_Id",
                        x => x.Id,
                        "Library",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "JellyfinLibrary");
    }
}
