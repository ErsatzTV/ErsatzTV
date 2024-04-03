using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Rework_PlayoutTemplate_ActiveDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "PlayoutTemplate");

            migrationBuilder.AddColumn<int>(
                name: "EndDay",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndMonth",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LimitToDateRange",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartDay",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartMonth",
                table: "PlayoutTemplate",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDay",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "LimitToDateRange",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "StartDay",
                table: "PlayoutTemplate");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                table: "PlayoutTemplate");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                table: "PlayoutTemplate",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                table: "PlayoutTemplate",
                type: "TEXT",
                nullable: true);
        }
    }
}
