using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Remove_ChannelLogo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "Logo",
                "Channel");

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<string>(
                "Logo",
                "Channel",
                "TEXT",
                nullable: true);
    }
}
