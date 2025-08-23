using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutPostRollHandler : IYamlPlayoutHandler
{
    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<SequentialPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutPostRollInstruction postRoll)
        {
            return Task.FromResult(false);
        }

        if (postRoll.PostRoll && !string.IsNullOrWhiteSpace(postRoll.Sequence))
        {
            context.SetPostRollSequence(postRoll.Sequence);
        }
        else
        {
            context.ClearPostRollSequence();
        }

        return Task.FromResult(true);
    }
}
