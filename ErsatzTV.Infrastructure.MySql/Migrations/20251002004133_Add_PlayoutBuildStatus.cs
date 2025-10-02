using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
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
                    PlayoutId = table.Column<int>(type: "int", nullable: false),
                    LastBuild = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutBuildStatus");
        }
    }
}
