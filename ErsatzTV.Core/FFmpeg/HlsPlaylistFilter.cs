using System;
using System.Globalization;
using System.Text;

namespace ErsatzTV.Core.FFmpeg
{
    public class HlsPlaylistFilter
    {
        public static TrimPlaylistResult TrimPlaylist(
            DateTimeOffset playlistStart,
            DateTimeOffset filterBefore,
            string[] lines,
            int maxSegments = 10,
            bool endWithDiscontinuity = false)
        {
            DateTimeOffset currentTime = playlistStart;
            DateTimeOffset nextPlaylistStart = DateTimeOffset.MaxValue;
            
            var discontinuitySequence = 0;
            var startSequence = 0;
            var output = new StringBuilder();
            var started = false;
            var i = 0;
            var segments = 0;
            while (!lines[i].StartsWith("#EXTINF:"))
            {
                if (lines[i].StartsWith("#EXT-X-DISCONTINUITY-SEQUENCE"))
                {
                    discontinuitySequence = int.Parse(lines[i].Split(':')[1]);
                }

                i++;
            }

            while (i < lines.Length)
            {
                if (segments >= maxSegments)
                {
                    break;
                }

                string line = lines[i];
                // _logger.LogInformation("Line: {Line}", line);
                if (line.StartsWith("#EXT-X-DISCONTINUITY"))
                {
                    if (started)
                    {
                        output.AppendLine("#EXT-X-DISCONTINUITY");
                    }
                    else
                    {
                        discontinuitySequence++;
                    }

                    i++;
                    continue;
                }

                var duration = TimeSpan.FromSeconds(
                    double.Parse(
                        lines[i].TrimEnd(',').Split(':')[1],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture));
                if (currentTime < filterBefore)
                {
                    currentTime += duration;
                    i += 3;
                    continue;
                }
    
                nextPlaylistStart = currentTime < nextPlaylistStart ? currentTime : nextPlaylistStart;

                if (!started)
                {
                    startSequence = int.Parse(lines[i + 2].Replace("live", string.Empty).Split('.')[0]);

                    output.AppendLine("#EXTM3U");
                    output.AppendLine("#EXT-X-VERSION:6");
                    output.AppendLine("#EXT-X-TARGETDURATION:4");
                    output.AppendLine($"#EXT-X-MEDIA-SEQUENCE:{startSequence}");
                    output.AppendLine($"#EXT-X-DISCONTINUITY-SEQUENCE:{discontinuitySequence}");
                    output.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");
                    output.AppendLine("#EXT-X-DISCONTINUITY");

                    started = true;
                }

                output.AppendLine(lines[i]);
                string offset = currentTime.ToString("zzz").Replace(":", string.Empty);
                output.AppendLine($"#EXT-X-PROGRAM-DATE-TIME:{currentTime:yyyy-MM-ddTHH:mm:ss.fff}{offset}");
                output.AppendLine(lines[i + 2]);

                currentTime += duration;
                segments++;
                i += 3;
            }

            if (endWithDiscontinuity)
            {
                output.AppendLine("#EXT-X-DISCONTINUITY");
            }

            return new TrimPlaylistResult(nextPlaylistStart, startSequence, output.ToString());
        }

        public static TrimPlaylistResult TrimPlaylistWithDiscontinuity(
            DateTimeOffset playlistStart,
            DateTimeOffset filterBefore,
            string[] lines)
        {
            return TrimPlaylist(playlistStart, filterBefore, lines, int.MaxValue, true);
        }
    }

    public record TrimPlaylistResult(DateTimeOffset PlaylistStart, int Sequence, string Playlist);
}
