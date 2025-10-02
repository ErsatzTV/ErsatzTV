using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutBuildStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayoutBuildStatus",
                columns: table => new
                {
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastBuild = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutBuildStatus", x => x.PlayoutId);
                    table.ForeignKey(
                        name: "FK_PlayoutBuildStatus_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutBuildStatus");
        }
    }
}
