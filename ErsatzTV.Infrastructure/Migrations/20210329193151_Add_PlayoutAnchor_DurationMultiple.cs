using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Add_PlayoutAnchor_DurationMultiple : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                "Anchor_DurationFinish",
                "Playout",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Anchor_MultipleRemaining",
                "Playout",
                "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Anchor_DurationFinish",
                "Playout");

            migrationBuilder.DropColumn(
                "Anchor_MultipleRemaining",
                "Playout");
        }
    }
}
