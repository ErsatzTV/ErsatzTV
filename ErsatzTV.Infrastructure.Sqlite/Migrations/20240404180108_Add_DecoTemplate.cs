using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Add_DecoTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecoTemplateId",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DecoTemplateGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoTemplateGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecoTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DecoTemplateGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecoTemplate_DecoTemplateGroup_DecoTemplateGroupId",
                        column: x => x.DecoTemplateGroupId,
                        principalTable: "DecoTemplateGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecoTemplateItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DecoTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    DecoId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoTemplateItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecoTemplateItem_DecoTemplate_DecoTemplateId",
                        column: x => x.DecoTemplateId,
                        principalTable: "DecoTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecoTemplateItem_Deco_DecoId",
                        column: x => x.DecoId,
                        principalTable: "Deco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutTemplate_DecoTemplateId",
                table: "PlayoutTemplate",
                column: "DecoTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplate_DecoTemplateGroupId",
                table: "DecoTemplate",
                column: "DecoTemplateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplate_Name",
                table: "DecoTemplate",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplateGroup_Name",
                table: "DecoTemplateGroup",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplateItem_DecoId",
                table: "DecoTemplateItem",
                column: "DecoId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplateItem_DecoTemplateId",
                table: "DecoTemplateItem",
                column: "DecoTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutTemplate_DecoTemplate_DecoTemplateId",
                table: "PlayoutTemplate",
                column: "DecoTemplateId",
                principalTable: "DecoTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutTemplate_DecoTemplate_DecoTemplateId",
                table: "PlayoutTemplate");

            migrationBuilder.DropTable(
                name: "DecoTemplateItem");

            migrationBuilder.DropTable(
                name: "DecoTemplate");

            migrationBuilder.DropTable(
                name: "DecoTemplateGroup");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutTemplate_DecoTemplateId",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "DecoTemplateId",
                table: "PlayoutTemplate");
        }
    }
}
