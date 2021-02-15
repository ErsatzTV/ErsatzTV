using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class MetadataOptimizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                "Metadata_LastWriteTime",
                "MediaItems",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Metadata_Source",
                "MediaItems",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "PosterLastWriteTime",
                "MediaItems",
                "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Metadata_LastWriteTime",
                "MediaItems");

            migrationBuilder.DropColumn(
                "Metadata_Source",
                "MediaItems");

            migrationBuilder.DropColumn(
                "PosterLastWriteTime",
                "MediaItems");
        }
    }
}
