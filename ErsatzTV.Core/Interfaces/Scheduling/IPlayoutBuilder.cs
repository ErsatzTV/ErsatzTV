using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IPlayoutBuilder
{
    public bool TrimStart { get; set; }
    public Playlist DebugPlaylist { get; set; }

    Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken);
}
