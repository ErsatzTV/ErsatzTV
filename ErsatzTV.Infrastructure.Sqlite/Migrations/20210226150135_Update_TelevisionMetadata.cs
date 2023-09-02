using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Update_TelevisionMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

            migrationBuilder.Sql(
                $@"INSERT INTO ShowMetadata (Outline, Plot, Tagline, ShowId, MetadataKind, Title, OriginalTitle, SortTitle, ReleaseDate, DateAdded, DateUpdated)
SELECT null, tsm.Plot, null, m.Id, tsm.Source, tsm.Title, null, tsm.SortTitle, tsm.Year || '-01-01 00:00:00', '{now}', '0001-01-01 00:00:00'
FROM TelevisionShowMetadata tsm
INNER JOIN MediaItem m ON tsm.TelevisionShowId = m.TelevisionShowId");

            migrationBuilder.Sql(
                $@"INSERT INTO EpisodeMetadata (Outline, Plot, Tagline, EpisodeId, MetadataKind, Title, OriginalTitle, SortTitle, ReleaseDate, DateAdded, DateUpdated)
SELECT null, tem.Plot, null, m.Id, tem.Source, tem.Title, null, tem.SortTitle, tem.Aired, '{now}', IFNULL(tem.LastWriteTime, '0001-01-01 00:00:00')
FROM TelevisionEpisodeMetadata tem
INNER JOIN MediaItem m ON tem.TelevisionEpisodeId = m.TelevisionEpisodeId;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
