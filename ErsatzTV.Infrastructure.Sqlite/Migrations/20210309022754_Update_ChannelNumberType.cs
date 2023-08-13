using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_ChannelNumberType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AlterColumn<string>(
                "Number",
                "Channel",
                "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AlterColumn<int>(
                "Number",
                "Channel",
                "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
    }
}
