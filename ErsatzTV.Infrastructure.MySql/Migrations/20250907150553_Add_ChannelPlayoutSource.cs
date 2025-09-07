using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Add_ChannelPlayoutSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MirrorSourceChannelId",
                table: "Channel",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PlayoutOffset",
                table: "Channel",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlayoutSource",
                table: "Channel",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Channel_MirrorSourceChannelId",
                table: "Channel",
                column: "MirrorSourceChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_Channel_MirrorSourceChannelId",
                table: "Channel",
                column: "MirrorSourceChannelId",
                principalTable: "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_Channel_MirrorSourceChannelId",
                table: "Channel");

            migrationBuilder.DropIndex(
                name: "IX_Channel_MirrorSourceChannelId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "MirrorSourceChannelId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "PlayoutOffset",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "PlayoutSource",
                table: "Channel");
        }
    }
}
