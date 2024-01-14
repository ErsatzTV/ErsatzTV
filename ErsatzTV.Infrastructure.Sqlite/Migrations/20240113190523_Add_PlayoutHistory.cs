using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayoutHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    When = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutHistory_Block_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Block",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutHistory_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutHistory_BlockId",
                table: "PlayoutHistory",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutHistory_PlayoutId",
                table: "PlayoutHistory",
                column: "PlayoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutHistory");
        }
    }
}
