using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_RemoteStreamFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Duration",
                table: "RemoteStream",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FallbackQuery",
                table: "RemoteStream",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "RemoteStream",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "RemoteStream",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration",
                table: "RemoteStream");

            migrationBuilder.DropColumn(
                name: "FallbackQuery",
                table: "RemoteStream");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "RemoteStream");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "RemoteStream");
        }
    }
}
