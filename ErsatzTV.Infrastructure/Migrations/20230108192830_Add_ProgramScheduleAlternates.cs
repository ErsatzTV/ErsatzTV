using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramScheduleAlternates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramScheduleAlternate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramScheduleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    DaysOfWeek = table.Column<string>(type: "TEXT", nullable: true),
                    DaysOfMonth = table.Column<string>(type: "TEXT", nullable: true),
                    MonthsOfYear = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramScheduleAlternate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleAlternate_Playout_PlayoutId",
                        column: x => x.PlayoutId,
                        principalTable: "Playout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramScheduleAlternate_ProgramSchedule_ProgramScheduleId",
                        column: x => x.ProgramScheduleId,
                        principalTable: "ProgramSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleAlternate_PlayoutId",
                table: "ProgramScheduleAlternate",
                column: "PlayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramScheduleAlternate_ProgramScheduleId",
                table: "ProgramScheduleAlternate",
                column: "ProgramScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgramScheduleAlternate");
        }
    }
}
