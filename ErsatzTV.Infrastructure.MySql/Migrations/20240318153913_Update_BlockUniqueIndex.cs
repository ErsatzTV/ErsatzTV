using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Update_BlockUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropIndex(
            //     name: "IX_Block_BlockGroupId",
            //     table: "Block");

            migrationBuilder.DropIndex(
                name: "IX_Block_Name",
                table: "Block");

            migrationBuilder.CreateIndex(
                name: "IX_Block_BlockGroupId_Name",
                table: "Block",
                columns: new[] { "BlockGroupId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Block_BlockGroupId_Name",
                table: "Block");

            // migrationBuilder.CreateIndex(
            //     name: "IX_Block_BlockGroupId",
            //     table: "Block",
            //     column: "BlockGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Block_Name",
                table: "Block",
                column: "Name",
                unique: true);
        }
    }
}
