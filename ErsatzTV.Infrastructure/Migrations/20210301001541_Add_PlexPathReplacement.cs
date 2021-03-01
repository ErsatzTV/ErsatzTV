using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlexPathReplacement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "PlexPathReplacement",
                table => new
                {
                    Id = table.Column<int>("INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlexPath = table.Column<string>("TEXT", nullable: true),
                    LocalPath = table.Column<string>("TEXT", nullable: true),
                    PlexMediaSourceId = table.Column<int>("INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlexPathReplacement", x => x.Id);
                    table.ForeignKey(
                        "FK_PlexPathReplacement_PlexMediaSource_PlexMediaSourceId",
                        x => x.PlexMediaSourceId,
                        "PlexMediaSource",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_PlexPathReplacement_PlexMediaSourceId",
                "PlexPathReplacement",
                "PlexMediaSourceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "PlexPathReplacement");
    }
}
