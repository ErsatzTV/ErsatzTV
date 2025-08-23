using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.ScriptedScheduling;

public class ScriptedPlayoutBuilder(ILogger<ScriptedPlayoutBuilder> logger) : IScriptedPlayoutBuilder
{
    public async Task<PlayoutBuildResult> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);

        logger.LogInformation("Building scripted playout...");

        return PlayoutBuildResult.Empty;
    }
}
