using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class BlockPlayoutBuilder(ILogger<BlockPlayoutBuilder> logger) : IBlockPlayoutBuilder
{
    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);

        logger.LogDebug(
            "Building block playout for channel {Number} - {Name}",
            playout.Channel.Number,
            playout.Channel.Name);

        return playout;
    }
}
