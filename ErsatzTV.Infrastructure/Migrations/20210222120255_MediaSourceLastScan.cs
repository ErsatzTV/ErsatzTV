using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class MediaSourceLastScan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<DateTimeOffset>(
                "LastScan",
                "MediaSources",
                "TEXT",
                nullable: true);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(
                "LastScan",
                "MediaSources");
    }
}
