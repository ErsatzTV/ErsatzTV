using System;

namespace ErsatzTV.Core.FFmpeg;

public interface IHlsPlaylistFilter
{
    TrimPlaylistResult TrimPlaylist(
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        string[] lines,
        int maxSegments = 10,
        bool endWithDiscontinuity = false);

    TrimPlaylistResult TrimPlaylistWithDiscontinuity(
        DateTimeOffset playlistStart,
        DateTimeOffset filterBefore,
        string[] lines);
}
