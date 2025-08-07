using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutBuilder
{
    public bool TrimStart { get; set; }
    public Playlist DebugPlaylist { get; set; }

    Task<PlayoutBuildResult> Build(
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken);
}
