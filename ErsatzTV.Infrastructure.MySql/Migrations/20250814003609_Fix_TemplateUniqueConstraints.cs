using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Fix_TemplateUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Template_Name",
                table: "Template");

            migrationBuilder.DropForeignKey(
                name: "FK_Template_TemplateGroup_TemplateGroupId",
                table: "Template");

            migrationBuilder.DropIndex(
                name: "IX_Template_TemplateGroupId",
                table: "Template");

            migrationBuilder.DropForeignKey(
                name: "FK_DecoTemplate_DecoTemplateGroup_DecoTemplateGroupId",
                table: "DecoTemplate");

            migrationBuilder.DropIndex(
                name: "IX_DecoTemplate_DecoTemplateGroupId",
                table: "DecoTemplate");

            migrationBuilder.DropIndex(
                name: "IX_DecoTemplate_Name",
                table: "DecoTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_Template_TemplateGroupId_Name",
                table: "Template",
                columns: new[] { "TemplateGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplate_DecoTemplateGroupId_Name",
                table: "DecoTemplate",
                columns: new[] { "DecoTemplateGroupId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Template_TemplateGroup_TemplateGroupId",
                table: "Template",
                column: "TemplateGroupId",
                principalTable: "TemplateGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DecoTemplate_DecoTemplateGroup_DecoTemplateGroupId",
                table: "DecoTemplate",
                column: "DecoTemplateGroupId",
                principalTable: "DecoTemplateGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Template_TemplateGroupId_Name",
                table: "Template");

            migrationBuilder.DropIndex(
                name: "IX_DecoTemplate_DecoTemplateGroupId_Name",
                table: "DecoTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_Template_Name",
                table: "Template",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Template_TemplateGroupId",
                table: "Template",
                column: "TemplateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplate_DecoTemplateGroupId",
                table: "DecoTemplate",
                column: "DecoTemplateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoTemplate_Name",
                table: "DecoTemplate",
                column: "Name",
                unique: true);
        }
    }
}
