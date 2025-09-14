using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutBuilder
{
    bool TrimStart { get; set; }
    Playlist DebugPlaylist { get; set; }

    Task<PlayoutBuildResult> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken);
}
