using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Fix_PlayoutAnchorInFlood : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Playout SET Anchor_InFlood = 0 WHERE Anchor_InFlood IS NULL");

            migrationBuilder.AlterColumn<bool>(
                name: "Anchor_InFlood",
                table: "Playout",
                type: "INTEGER",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
