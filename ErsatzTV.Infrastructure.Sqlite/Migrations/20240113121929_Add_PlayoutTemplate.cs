using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_PlayoutTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Template_Playout_PlayoutId",
                table: "Template");

            migrationBuilder.DropIndex(
                name: "IX_Template_PlayoutId",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "PlayoutId",
                table: "Template");

            migrationBuilder.CreateTable(
                name: "PlayoutTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "TEXT", nullable: true),
                    MonthsOfYear = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayoutTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayoutTemplate_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayoutTemplate_Template_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Template",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutTemplate_PlayoutId",
                table: "PlayoutTemplate",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutTemplate_TemplateId",
                table: "PlayoutTemplate",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayoutTemplate");

            migrationBuilder.AddColumn<int>(
                name: "PlayoutId",
                table: "Template",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Template_PlayoutId",
                table: "Template",
                column: "PlayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_Template_Playout_PlayoutId",
                table: "Template",
                column: "PlayoutId",
                principalTable: "Playout",
                principalColumn: "Id");
        }
    }
}
