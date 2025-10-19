using System.Globalization;
using System.Text;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg.OutputFormat;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class HlsPlaylistFilter(ITempFilePool tempFilePool, ILogger<HlsPlaylistFilter> logger) : IHlsPlaylistFilter
{
    public TrimPlaylistResult TrimPlaylist(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        IHlsInitSegmentCache hlsInitSegmentCache,
        string[] lines,
        Option<int> maybeMaxSegments,
        bool endWithDiscontinuity = false)
    {
        try
        {
            List<PlaylistItem> items = [];

            DateTimeOffset currentTime = playlistStart;

            var targetDuration = 0;
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
                else if (lines[i].StartsWith("#EXT-X-TARGETDURATION", StringComparison.OrdinalIgnoreCase))
                {
                    targetDuration = int.Parse(lines[i].Split(':')[1], CultureInfo.InvariantCulture);
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

                if (lines[i].StartsWith("#EXT-X-MAP:URI=", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    continue;
                }

                var durationDecimal = decimal.Parse(
                    lines[i].TrimEnd(',').Split(':')[1],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture);
                var duration = TimeSpan.FromTicks((long)(durationDecimal * TimeSpan.TicksPerSecond));

                long segmentNameTimeSeconds = long.MaxValue;
                if (outputFormat is OutputFormatKind.HlsMp4)
                {
                    if (!lines[i + 2].Contains('_') || !long.TryParse(
                            lines[i + 2].Split('_')[1],
                            out segmentNameTimeSeconds))
                    {
                        segmentNameTimeSeconds = long.MaxValue;
                    }
                }

                items.Add(new PlaylistSegment(currentTime, segmentNameTimeSeconds, lines[i], lines[i + 2]));

                currentTime += duration;
                i += 3;
            }

            if (endWithDiscontinuity && items[^1] is not PlaylistDiscontinuity)
            {
                items.Add(new PlaylistDiscontinuity());
            }

            (string playlist, DateTimeOffset nextPlaylistStart, long startSequence, long generatedAt, int segments) =
                GeneratePlaylist(
                    outputFormat,
                    items,
                    hlsInitSegmentCache,
                    filterBefore,
                    targetDuration,
                    discontinuitySequence,
                    maybeMaxSegments);

            return new TrimPlaylistResult(nextPlaylistStart, startSequence, generatedAt, playlist, segments);
        }
        catch (Exception ex)
        {
            try
            {
                string file = tempFilePool.GetNextTempFile(TempFileCategory.BadPlaylist);
                File.WriteAllLines(file, lines);

                logger.LogError(ex, "Error filtering playlist. Bad playlist saved to {BadPlaylistFile}", file);

                // TODO: better error result?
                return new TrimPlaylistResult(playlistStart, 0, 0, string.Empty, 0);
            }
            catch
            {
                // do nothing
            }

            throw;
        }
    }

    public TrimPlaylistResult TrimPlaylistWithDiscontinuity(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        IHlsInitSegmentCache hlsInitSegmentCache,
        string[] lines) =>
        TrimPlaylist(outputFormat, playlistStart, filterBefore, hlsInitSegmentCache, lines, Option<int>.None, true);

    private static Tuple<string, DateTimeOffset, long, long, int> GeneratePlaylist(
        OutputFormatKind outputFormat,
        List<PlaylistItem> items,
        IHlsInitSegmentCache hlsInitSegmentCache,
        DateTimeOffset filterBefore,
        int targetDuration,
        int discontinuitySequence,
        Option<int> maybeMaxSegments)
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

        // still need to filter old content
        if (maybeMaxSegments.IsNone)
        {
            allSegments.RemoveAll(s => s.StartTime < filterBefore);
        }

        foreach (int maxSegments in maybeMaxSegments)
        {
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
        }

        long startSequence = 0;
        long generatedAt = 0;
        foreach (var startSegment in allSegments.HeadOrNone())
        {
            startSequence = startSegment.StartSequence;
            generatedAt = startSegment.GeneratedAt;
        }

        // count all discontinuities that were filtered out
        if (allSegments.Count != 0)
        {
            int index = items.IndexOf(allSegments.Head());
            int count = items.Take(index + 1).OfType<PlaylistDiscontinuity>().Count();
            discontinuitySequence += count;
        }

        var output = new StringBuilder();
        output.AppendLine("#EXTM3U");
        output.AppendLine("#EXT-X-VERSION:7");
        output.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-TARGETDURATION:{targetDuration}");
        output.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-MEDIA-SEQUENCE:{startSequence}");
        output.AppendLine(CultureInfo.InvariantCulture, $"#EXT-X-DISCONTINUITY-SEQUENCE:{discontinuitySequence}");
        output.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        if (outputFormat is OutputFormatKind.HlsMp4)
        {
            output.AppendLine(
                CultureInfo.InvariantCulture,
                $"#EXT-X-MAP:URI=\"{hlsInitSegmentCache.EarliestSegmentByHash(generatedAt)}\"");
        }

        for (var i = 0; i < items.Count; i++)
        {
            switch (items[i])
            {
                case PlaylistDiscontinuity:
                    if (i < items.Count - 1 && allSegments.Contains(items[i + 1]))
                    {
                        if (items[i + 1] is PlaylistSegment nextSegment && allSegments.Head() != nextSegment)
                        {
                            output.AppendLine("#EXT-X-DISCONTINUITY");

                            if (outputFormat is OutputFormatKind.HlsMp4)
                            {
                                output.AppendLine(
                                    CultureInfo.InvariantCulture,
                                    $"#EXT-X-MAP:URI=\"{hlsInitSegmentCache.EarliestSegmentByHash(nextSegment.GeneratedAt)}\"");
                            }
                        }
                    }
                    else if (i == items.Count - 1 && allSegments.Count > 0) // discontinuity at the end
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

        return Tuple(
            playlist,
            nextPlaylistStart,
            startSequence,
            items.OfType<PlaylistSegment>().Min(s => s.GeneratedAt),
            allSegments.Count);
    }

    private abstract record PlaylistItem;

    private record PlaylistSegment(DateTimeOffset StartTime, long GeneratedAt, string ExtInf, string Line)
        : PlaylistItem
    {
        public long StartSequence => long.Parse(
            Line.Contains('_') ? Line.Split('_')[2].Split('.')[0] : Line.Replace("live", string.Empty).Split('.')[0],
            CultureInfo.InvariantCulture);
    }

    private record PlaylistDiscontinuity : PlaylistItem;
}

public record TrimPlaylistResult(
    DateTimeOffset PlaylistStart,
    long Sequence,
    long GeneratedAt,
    string Playlist,
    int SegmentCount);
