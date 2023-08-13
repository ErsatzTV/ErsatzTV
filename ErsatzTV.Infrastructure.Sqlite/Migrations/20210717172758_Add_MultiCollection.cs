using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Add_MultiCollection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MultiCollectionId",
                table: "ProgramScheduleItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MultiCollection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MultiCollectionItem",
                columns: table => new
                {
                    MultiCollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleAsGroup = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlaybackOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiCollectionItem", x => new { x.MultiCollectionId, x.CollectionId });
                    table.ForeignKey(
                        name: "FK_MultiCollectionItem_Collection_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiCollectionItem_MultiCollection_MultiCollectionId",
                        column: x => x.MultiCollectionId,
                        principalTable: "MultiCollection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleItem_MultiCollectionId",
                table: "ProgramScheduleItem",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayoutProgramScheduleAnchor_MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "MultiCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiCollectionItem_CollectionId",
                table: "MultiCollectionItem",
                column: "CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_MultiCollection_MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor",
                column: "MultiCollectionId",
                principalTable: "MultiCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramScheduleItem_MultiCollection_MultiCollectionId",
                table: "ProgramScheduleItem",
                column: "MultiCollectionId",
                principalTable: "MultiCollection",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayoutProgramScheduleAnchor_MultiCollection_MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramScheduleItem_MultiCollection_MultiCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropTable(
                name: "MultiCollectionItem");

            migrationBuilder.DropTable(
                name: "MultiCollection");

            migrationBuilder.DropIndex(
                name: "IX_ProgramScheduleItem_MultiCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropIndex(
                name: "IX_PlayoutProgramScheduleAnchor_MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor");

            migrationBuilder.DropColumn(
                name: "MultiCollectionId",
                table: "ProgramScheduleItem");

            migrationBuilder.DropColumn(
                name: "MultiCollectionId",
                table: "PlayoutProgramScheduleAnchor");
        }
    }
}
