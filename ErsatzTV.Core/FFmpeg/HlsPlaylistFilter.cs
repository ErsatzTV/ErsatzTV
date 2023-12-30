using System.Globalization;
using System.Text;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class HlsPlaylistFilter : IHlsPlaylistFilter
{
    private readonly ILogger<HlsPlaylistFilter> _logger;
    private readonly ITempFilePool _tempFilePool;

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
            List<PlaylistItem> items = new();

            DateTimeOffset currentTime = playlistStart;

            var discontinuitySequence = 0;
            var i = 0;
            while (!lines[i].StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                if (lines[i].StartsWith("#EXT-X-DISCONTINUITY-SEQUENCE", StringComparison.OrdinalIgnoreCase))
                {
                    discontinuitySequence = int.Parse(lines[i].Split(':')[1], CultureInfo.InvariantCulture);
                }
                else if (lines[i].StartsWith("#EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new PlaylistDiscontinuity());
                }

                i++;
            }

            while (i < lines.Length)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                if (line.StartsWith("#EXT-X-DISCONTINUITY", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(new PlaylistDiscontinuity());
                    i++;
                    continue;
                }

                var duration = TimeSpan.FromSeconds(
                    double.Parse(
                        lines[i].TrimEnd(',').Split(':')[1],
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture));

                items.Add(new PlaylistSegment(currentTime, lines[i], lines[i + 2]));

                currentTime += duration;
                i += 3;
            }

            if (endWithDiscontinuity && items[^1] is not PlaylistDiscontinuity)
            {
                items.Add(new PlaylistDiscontinuity());
            }

            (string playlist, DateTimeOffset nextPlaylistStart, int startSequence, int segments) = GeneratePlaylist(
                items,
                filterBefore,
                discontinuitySequence,
                maxSegments);

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
        string[] lines) =>
        TrimPlaylist(playlistStart, filterBefore, lines, int.MaxValue, true);

    private static Tuple<string, DateTimeOffset, int, int> GeneratePlaylist(
        List<PlaylistItem> items,
        DateTimeOffset filterBefore,
        int discontinuitySequence,
        int maxSegments)
    {
        if (items.Count != 0 && items[0] is PlaylistDiscontinuity)
        {
            discontinuitySequence++;
        }

        while (items.Count != 0 && items[0] is PlaylistDiscontinuity)
        {
            items.RemoveAt(0);
        }

        var allSegments = items.OfType<PlaylistSegment>().ToList();
        // only filter if we have more than requested
        if (allSegments.Count > maxSegments)
        {
            var afterFilter = allSegments.Filter(s => s.StartTime >= filterBefore).ToList();

            // if there are enough new segments after filtering, use those
            // otherwise return the last maxSegments
            allSegments = afterFilter.Count >= maxSegments
                ? afterFilter.Take(maxSegments).ToList()
                : allSegments.TakeLast(maxSegments).ToList();
        }

        int startSequence = allSegments
            .HeadOrNone()
            .Map(s => s.StartSequence)
            .IfNone(0);

        // count all discontinuities that were filtered out
        if (allSegments.Count != 0)
        {
            int index = items.IndexOf(allSegments.Head());
            int count = items.Take(index + 1).OfType<PlaylistDiscontinuity>().Count();
            discontinuitySequence += count;
        }

        var output = new StringBuilder();
        output.AppendLine("#EXTM3U");
        output.AppendLine("#EXT-X-VERSION:6");
        output.AppendLine("#EXT-X-TARGETDURATION:4");
        output.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-MEDIA-SEQUENCE:{startSequence}");
        output.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-DISCONTINUITY-SEQUENCE:{discontinuitySequence}");
        output.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        for (var i = 0; i < items.Count; i++)
        {
            switch (items[i])
            {
                case PlaylistDiscontinuity:
                    if (i == items.Count - 1 || allSegments.Contains(items[i + 1]))
                    {
                        output.AppendLine("#EXT-X-DISCONTINUITY");
                    }

                    break;
                case PlaylistSegment segment:
                    if (allSegments.Contains(segment))
                    {
                        output.AppendLine(segment.ExtInf);
                        string offset = segment.StartTime
                            .ToString("zzz", CultureInfo.InvariantCulture)
                            .Replace(":", string.Empty);
                        output.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"#EXT-X-PROGRAM-DATE-TIME:{segment.StartTime:yyyy-MM-ddTHH:mm:ss.fff}{offset}");
                        output.AppendLine(segment.Line);
                    }

                    break;
            }
        }

        var playlist = output.ToString();
        DateTimeOffset nextPlaylistStart = allSegments.HeadOrNone()
            .Map(s => s.StartTime)
            .IfNone(DateTimeOffset.MaxValue);

        return Tuple(playlist, nextPlaylistStart, startSequence, allSegments.Count);
    }

    private abstract record PlaylistItem;

    private record PlaylistSegment(DateTimeOffset StartTime, string ExtInf, string Line) : PlaylistItem
    {
        public int StartSequence => int.Parse(
            Line.Replace("live", string.Empty).Split('.')[0],
            CultureInfo.InvariantCulture);
    }

    private record PlaylistDiscontinuity : PlaylistItem;
}

public record TrimPlaylistResult(DateTimeOffset PlaylistStart, int Sequence, string Playlist, int SegmentCount);
