using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutMidRollHandler : IYamlPlayoutHandler
{
    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlMidRollInstruction midRoll)
        {
            return Task.FromResult(false);
        }

        if (midRoll.MidRoll && !string.IsNullOrWhiteSpace(midRoll.Sequence) && !string.IsNullOrWhiteSpace(midRoll.Expression))
        {
            context.SetMidRollSequence(new YamlPlayoutContext.MidRollSequence(midRoll.Sequence, midRoll.Expression));
        }
        else
        {
            context.ClearMidRollSequence();
        }

        return Task.FromResult(true);
    }
}
