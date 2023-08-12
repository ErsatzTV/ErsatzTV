using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class CleanUp_MediaItemStatisticsAndPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_MediaItem_Path",
                "MediaItem");

            migrationBuilder.DropColumn(
                "LastWriteTime",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Path",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_AudioCodec",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_DisplayAspectRatio",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_Duration",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_Height",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_LastWriteTime",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_SampleAspectRatio",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_VideoCodec",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_VideoScanType",
                "MediaItem");

            migrationBuilder.DropColumn(
                "Statistics_Width",
                "MediaItem");

            migrationBuilder.DropColumn(
                "TelevisionEpisodeId",
                "MediaItem");

            migrationBuilder.DropColumn(
                "TelevisionSeasonId",
                "MediaItem");

            migrationBuilder.DropColumn(
                "TelevisionShowId",
                "MediaItem");

            migrationBuilder.AddColumn<DateTime>(
                "DateAdded",
                "MediaVersion",
                "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                "DateUpdated",
                "MediaVersion",
                "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "DateAdded",
                "MediaVersion");

            migrationBuilder.DropColumn(
                "DateUpdated",
                "MediaVersion");

            migrationBuilder.AddColumn<DateTime>(
                "LastWriteTime",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Path",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Statistics_AudioCodec",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Statistics_DisplayAspectRatio",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                "Statistics_Duration",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Statistics_Height",
                "MediaItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "Statistics_LastWriteTime",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Statistics_SampleAspectRatio",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Statistics_VideoCodec",
                "MediaItem",
                "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Statistics_VideoScanType",
                "MediaItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "Statistics_Width",
                "MediaItem",
                "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "TelevisionEpisodeId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "TelevisionSeasonId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "TelevisionShowId",
                "MediaItem",
                "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                "IX_MediaItem_Path",
                "MediaItem",
                "Path",
                unique: true);
        }
    }
}
