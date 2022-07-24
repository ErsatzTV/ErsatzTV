using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations;

public partial class Add_Resolution640480 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("INSERT INTO Resolution (Id, Height, Width, Name) VALUES (0, 480, 640, '640x480')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}