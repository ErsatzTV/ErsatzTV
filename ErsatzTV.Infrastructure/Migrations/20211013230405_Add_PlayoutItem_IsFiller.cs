using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlayoutItem_IsFiller : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFiller",
                table: "PlayoutItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFiller",
                table: "PlayoutItem");
        }
    }
}
