using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlayoutItemGuideGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CustomGroup",
                table: "PlayoutItem",
                newName: "GuideGroup");

            migrationBuilder.AddColumn<int>(
                name: "Anchor_NextGuideGroup",
                table: "Playout",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anchor_NextGuideGroup",
                table: "Playout");

            migrationBuilder.RenameColumn(
                name: "GuideGroup",
                table: "PlayoutItem",
                newName: "CustomGroup");
        }
    }
}
