using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_FFmpegProfileQsvAcceleration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<bool>(
                "QsvAcceleration",
                "FFmpegProfile",
                "INTEGER",
                nullable: false,
                defaultValue: false);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "QsvAcceleration",
                "FFmpegProfile");
    }
}
