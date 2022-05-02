using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_MusicVideoArtists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicVideoArtist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    MusicVideoMetadataId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicVideoArtist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicVideoArtist_MusicVideoMetadata_MusicVideoMetadataId",
                        column: x => x.MusicVideoMetadataId,
                        principalTable: "MusicVideoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicVideoArtist_MusicVideoMetadataId",
                table: "MusicVideoArtist",
                column: "MusicVideoMetadataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicVideoArtist");
        }
    }
}
