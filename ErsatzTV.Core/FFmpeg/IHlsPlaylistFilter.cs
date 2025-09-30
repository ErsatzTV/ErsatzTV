using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.OutputFormat;

namespace ErsatzTV.Core.FFmpeg;

public interface IHlsPlaylistFilter
{
    TrimPlaylistResult TrimPlaylist(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        List<long> inits,
        string[] lines,
        int maxSegments = 10,
        bool endWithDiscontinuity = false);

    TrimPlaylistResult TrimPlaylistWithDiscontinuity(
        OutputFormatKind outputFormat,
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        List<long> inits,
        string[] lines);
}
