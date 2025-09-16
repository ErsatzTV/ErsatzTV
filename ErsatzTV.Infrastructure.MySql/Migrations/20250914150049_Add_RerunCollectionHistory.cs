using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_RerunCollectionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RerunCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CollectionType = table.Column<int>(type: "int", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: true),
                    MediaItemId = table.Column<int>(type: "int", nullable: true),
                    MultiCollectionId = table.Column<int>(type: "int", nullable: true),
                    SmartCollectionId = table.Column<int>(type: "int", nullable: true),
                    FirstRunPlaybackOrder = table.Column<int>(type: "int", nullable: false),
                    RerunPlaybackOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RerunCollection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RerunCollection_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RerunCollection_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RerunCollection_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RerunCollection_SmartCollection_SmartCollectionId",
                        column: x => x.SmartCollectionId,
                        principalTable: "SmartCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RerunHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    RerunCollectionId = table.Column<int>(type: "int", nullable: false),
                    MediaItemId = table.Column<int>(type: "int", nullable: false),
                    When = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RerunHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RerunHistory_MediaItem_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RerunHistory_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RerunHistory_RerunCollection_RerunCollectionId",
                        column: x => x.RerunCollectionId,
                        principalTable: "RerunCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RerunCollection_CollectionId",
                table: "RerunCollection",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunCollection_MediaItemId",
                table: "RerunCollection",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunCollection_MultiCollectionId",
                table: "RerunCollection",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunCollection_SmartCollectionId",
                table: "RerunCollection",
                column: "SmartCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunHistory_MediaItemId",
                table: "RerunHistory",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunHistory_PlayoutId",
                table: "RerunHistory",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_RerunHistory_RerunCollectionId",
                table: "RerunHistory",
                column: "RerunCollectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RerunHistory");

            migrationBuilder.DropTable(
                name: "RerunCollection");
        }
    }
}
