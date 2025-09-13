using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutGap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayoutGap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Finish = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutGap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutGap_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutGap_PlayoutId",
                table: "PlayoutGap",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutGap_Start_Finish",
                table: "PlayoutGap",
                columns: new[] { "Start", "Finish" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutGap");
        }
    }
}
