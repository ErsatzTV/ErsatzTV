using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutPreRollHandler : IYamlPlayoutHandler
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
        if (instruction is not YamlPlayoutPreRollInstruction preRoll)
        {
            return Task.FromResult(false);
        }

        if (preRoll.PreRoll && !string.IsNullOrWhiteSpace(preRoll.Sequence))
        {
            context.SetPreRollSequence(preRoll.Sequence);
        }
        else
        {
            context.ClearPreRollSequence();
        }

        return Task.FromResult(true);
    }
}
