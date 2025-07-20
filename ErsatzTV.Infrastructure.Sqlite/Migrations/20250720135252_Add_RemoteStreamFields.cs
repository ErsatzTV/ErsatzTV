using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FallbackQuery",
                table: "RemoteStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "RemoteStream",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "RemoteStream",
                type: "TEXT",
                nullable: true);
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
