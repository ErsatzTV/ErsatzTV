using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IBlockPlayoutBuilder
{
    Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken);
}
