using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_DecoBreakContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BreakContentMode",
                table: "Deco",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DecoBreakContent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DecoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    MediaItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlaylistId = table.Column<int>(type: "INTEGER", nullable: true),
                    Placement = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoBreakContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_Deco_DecoId",
                        column: x => x.DecoId,
                        principalTable: "Deco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_Playlist_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "Playlist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoBreakContent_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_CollectionId",
                table: "DecoBreakContent",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_DecoId",
                table: "DecoBreakContent",
                column: "DecoId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_MediaItemId",
                table: "DecoBreakContent",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_MultiCollectionId",
                table: "DecoBreakContent",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_PlaylistId",
                table: "DecoBreakContent",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoBreakContent_SmartCollectionId",
                table: "DecoBreakContent",
                column: "SmartCollectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecoBreakContent");

            migrationBuilder.DropColumn(
                name: "BreakContentMode",
                table: "Deco");
        }
    }
}
