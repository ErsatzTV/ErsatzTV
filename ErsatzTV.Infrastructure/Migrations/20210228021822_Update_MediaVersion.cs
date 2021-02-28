using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Update_MediaVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // movie versions
            migrationBuilder.Sql(
                @"INSERT INTO MediaVersion (Name, Duration, SampleAspectRatio, DisplayAspectRatio, VideoCodec, AudioCodec, VideoScanKind, Width, Height, EpisodeId, MovieId)
                SELECT 'Main', mi.Statistics_Duration, mi.Statistics_SampleAspectRatio, mi.Statistics_DisplayAspectRatio, mi.Statistics_VideoCodec, mi.Statistics_AudioCodec, mi.Statistics_VideoScanType, mi.Statistics_Width, mi.Statistics_Height, null, m.Id
                FROM MediaItem mi
                INNER JOIN Movie m on m.Id = mi.Id");

            // episode versions
            migrationBuilder.Sql(
                @"INSERT INTO MediaVersion (Name, Duration, SampleAspectRatio, DisplayAspectRatio, VideoCodec, AudioCodec, VideoScanKind, Width, Height, EpisodeId, MovieId)
                SELECT 'Main', mi.Statistics_Duration, mi.Statistics_SampleAspectRatio, mi.Statistics_DisplayAspectRatio, mi.Statistics_VideoCodec, mi.Statistics_AudioCodec, mi.Statistics_VideoScanType, mi.Statistics_Width, mi.Statistics_Height, e.Id, null
                FROM MediaItem mi
                INNER JOIN Episode e on e.Id = mi.Id");

            // movie files
            migrationBuilder.Sql(
                @"INSERT INTO MediaFile (Path, MediaVersionId)
                SELECT mi.Path, mv.Id
                FROM MediaItem mi
                INNER JOIN MediaVersion mv ON mv.MovieId = mi.Id");

            // episode files
            migrationBuilder.Sql(
                @"INSERT INTO MediaFile (Path, MediaVersionId)
                SELECT mi.Path, mv.Id
                FROM MediaItem mi
                INNER JOIN MediaVersion mv ON mv.EpisodeId = mi.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
