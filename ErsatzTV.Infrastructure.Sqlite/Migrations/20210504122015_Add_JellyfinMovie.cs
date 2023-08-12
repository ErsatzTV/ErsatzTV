using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_JellyfinMovie : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.CreateTable(
                "JellyfinMovie",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<string>("TEXT", nullable: true),
                    Etag = table.Column<string>("TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JellyfinMovie", x => x.Id);
                    table.ForeignKey(
                        "FK_JellyfinMovie_Movie_Id",
                        x => x.Id,
                        "Movie",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "JellyfinMovie");
    }
}
