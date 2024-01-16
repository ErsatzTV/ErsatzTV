using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IBlockPlayoutBuilder
{
    Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken);

    Task<Playout> Build(
        Playout playout,
        PlayoutBuildMode mode,
        ILogger customLogger,
        int daysToBuild,
        bool randomizeStartPoints,
        CancellationToken cancellationToken);
}
