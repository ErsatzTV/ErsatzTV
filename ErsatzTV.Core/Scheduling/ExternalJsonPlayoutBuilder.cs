using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling;

public class ExternalJsonPlayoutBuilder(ILogger<ExternalJsonPlayoutBuilder> logger) : IExternalJsonPlayoutBuilder
{
    // nothing to do for external json playouts
    public Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Building external json playout for channel {Number} - {Name}",
            playout.Channel.Number,
            playout.Channel.Name);

        return Task.FromResult(playout);
    }
}
