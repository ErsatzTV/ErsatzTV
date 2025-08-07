using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IBlockPlayoutFillerBuilder
{
    Task<PlayoutBuildResult> Build(
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildResult result,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken);
}
