using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_Deco_Group_Deco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecoId",
                table: "Playout",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DecoGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecoGroup", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Deco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DecoGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WatermarkId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deco_ChannelWatermark_WatermarkId",
                        column: x => x.WatermarkId,
                        principalTable: "ChannelWatermark",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Deco_DecoGroup_DecoGroupId",
                        column: x => x.DecoGroupId,
                        principalTable: "DecoGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Playout_DecoId",
                table: "Playout",
                column: "DecoId");

            migrationBuilder.CreateIndex(
                name: "IX_Deco_DecoGroupId_Name",
                table: "Deco",
                columns: new[] { "DecoGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deco_WatermarkId",
                table: "Deco",
                column: "WatermarkId");

            migrationBuilder.CreateIndex(
                name: "IX_DecoGroup_Name",
                table: "DecoGroup",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Playout_Deco_DecoId",
                table: "Playout",
                column: "DecoId",
                principalTable: "Deco",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playout_Deco_DecoId",
                table: "Playout");

            migrationBuilder.DropTable(
                name: "Deco");

            migrationBuilder.DropTable(
                name: "DecoGroup");

            migrationBuilder.DropIndex(
                name: "IX_Playout_DecoId",
                table: "Playout");

            migrationBuilder.DropColumn(
                name: "DecoId",
                table: "Playout");
        }
    }
}
