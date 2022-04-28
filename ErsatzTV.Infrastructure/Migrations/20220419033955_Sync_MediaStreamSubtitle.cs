using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErsatzTV.Infrastructure.Migrations
{
    public partial class Sync_MediaStreamSubtitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");

            // delete all subtitles
            migrationBuilder.Sql("DELETE FROM Subtitle");
        
            // sync media stream (kind == 3/subtitles) to subtitles table
            migrationBuilder.Sql(
                $@"INSERT INTO Subtitle (Codec, `Default`, Forced, Language, StreamIndex, SubtitleKind, DateAdded, DateUpdated, EpisodeMetadataId)
                     SELECT Codec, `Default`, Forced, Language, `Index`, 0, '{now}', '{now}', EM.Id
                     FROM MediaStream
                     INNER JOIN MediaVersion MV on MV.Id = MediaStream.MediaVersionId
                     INNER JOIN EpisodeMetadata EM on MV.EpisodeId = EM.EpisodeId
                     WHERE MediaStreamKind = 3");

            migrationBuilder.Sql(
                $@"INSERT INTO Subtitle (Codec, `Default`, Forced, Language, StreamIndex, SubtitleKind, DateAdded, DateUpdated, MovieMetadataId)
                     SELECT Codec, `Default`, Forced, Language, `Index`, 0, '{now}', '{now}', MM.Id
                     FROM MediaStream
                     INNER JOIN MediaVersion MV on MV.Id = MediaStream.MediaVersionId
                     INNER JOIN MovieMetadata MM on MV.MovieId = MM.MovieId
                     WHERE MediaStreamKind = 3");
        
            migrationBuilder.Sql(
                $@"INSERT INTO Subtitle (Codec, `Default`, Forced, Language, StreamIndex, SubtitleKind, DateAdded, DateUpdated, MusicVideoMetadataId)
                     SELECT Codec, `Default`, Forced, Language, `Index`, 0, '{now}', '{now}', MVM.Id
                     FROM MediaStream
                     INNER JOIN MediaVersion MV on MV.Id = MediaStream.MediaVersionId
                     INNER JOIN MusicVideoMetadata MVM on MV.MusicVideoId = MVM.MusicVideoId
                     WHERE MediaStreamKind = 3");

            migrationBuilder.Sql(
                $@"INSERT INTO Subtitle (Codec, `Default`, Forced, Language, StreamIndex, SubtitleKind, DateAdded, DateUpdated, OtherVideoMetadataId)
                     SELECT Codec, `Default`, Forced, Language, `Index`, 0, '{now}', '{now}', OVM.Id
                     FROM MediaStream
                     INNER JOIN MediaVersion MV on MV.Id = MediaStream.MediaVersionId
                     INNER JOIN OtherVideoMetadata OVM on MV.OtherVideoId = OVM.OtherVideoId
                     WHERE MediaStreamKind = 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
