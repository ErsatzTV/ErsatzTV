using ErsatzTV.FFmpeg.OutputFormat;

namespace ErsatzTV.Core.FFmpeg;

public interface IHlsPlaylistFilter
{
    TrimPlaylistResult TrimPlaylist(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        IHlsInitSegmentCache hlsInitSegmentCache,
        string[] lines,
        Option<int> maybeMaxSegments,
        bool endWithDiscontinuity = false);

    TrimPlaylistResult TrimPlaylistWithDiscontinuity(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        IHlsInitSegmentCache hlsInitSegmentCache,
        string[] lines);
}
