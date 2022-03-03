using System.Globalization;
using System.Text;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class HlsPlaylistFilter : IHlsPlaylistFilter
{
    private readonly ITempFilePool _tempFilePool;
    private readonly ILogger<HlsPlaylistFilter> _logger;

    public HlsPlaylistFilter(ITempFilePool tempFilePool, ILogger<HlsPlaylistFilter> logger)
    {
        _tempFilePool = tempFilePool;
        _logger = logger;
    }

    public TrimPlaylistResult TrimPlaylist(
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        string[] lines,
        int maxSegments = 10,
        bool endWithDiscontinuity = false)
    {
        try
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

            var playlist = output.ToString();
            if (endWithDiscontinuity && !playlist.EndsWith($"#EXT-X-DISCONTINUITY{Environment.NewLine}"))
            {
                playlist += "#EXT-X-DISCONTINUITY" + Environment.NewLine;
            }

            return new TrimPlaylistResult(nextPlaylistStart, startSequence, playlist, segments);
        }
        catch (Exception ex)
        {
            try
            {
                string file = _tempFilePool.GetNextTempFile(TempFileCategory.BadPlaylist);
                File.WriteAllLines(file, lines);

                _logger.LogError(ex, "Error filtering playlist. Bad playlist saved to {BadPlaylistFile}", file);
                    
                // TODO: better error result?
                return new TrimPlaylistResult(playlistStart, 0, string.Empty, 0);
            }
            catch
            {
                // do nothing
            }

            throw;
        }
    }

    public TrimPlaylistResult TrimPlaylistWithDiscontinuity(
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        string[] lines)
    {
        return TrimPlaylist(playlistStart, filterBefore, lines, int.MaxValue, true);
    }
}

public record TrimPlaylistResult(DateTimeOffset PlaylistStart, int Sequence, string Playlist, int SegmentCount);