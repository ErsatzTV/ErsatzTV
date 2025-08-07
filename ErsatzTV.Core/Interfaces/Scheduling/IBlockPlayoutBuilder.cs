using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IBlockPlayoutBuilder
{
    Task<PlayoutBuildResult> Build(
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken);
}
