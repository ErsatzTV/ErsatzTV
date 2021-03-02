using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_MovieMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");

            migrationBuilder.Sql(
                @$"INSERT INTO NewMovieMetadata (Outline, Plot, Tagline, MovieId, MetadataKind, Title, OriginalTitle, SortTitle, ReleaseDate, DateAdded, DateUpdated)
SELECT mm.Outline, mm.Plot, mm.Tagline, m.Id, mm.Source, mm.Title, null, mm.SortTitle, mm.Premiered, '{now}', IFNULL(mm.LastWriteTime, '0001-01-01 00:00:00')
FROM MovieMetadata mm
INNER JOIN Movie m ON mm.MovieId = m.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
