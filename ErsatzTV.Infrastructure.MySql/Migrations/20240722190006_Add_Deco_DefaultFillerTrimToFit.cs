﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_Deco_DefaultFillerTrimToFit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultFillerTrimToFit",
                table: "Deco",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultFillerTrimToFit",
                table: "Deco");
        }
    }
}
