using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IBlockPlayoutFillerBuilder
{
    Task<Playout> Build(Playout playout, CancellationToken cancellationToken);
}
