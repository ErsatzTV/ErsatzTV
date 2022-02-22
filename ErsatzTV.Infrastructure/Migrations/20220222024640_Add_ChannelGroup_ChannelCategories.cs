using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_ChannelGroup_ChannelCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categories",
                table: "Channel",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Channel",
                type: "TEXT",
                nullable: false,
                defaultValue: "ErsatzTV");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categories",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Channel");
        }
    }
}
