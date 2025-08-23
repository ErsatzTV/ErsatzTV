using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutRepeatHandler : IYamlPlayoutHandler
{
    private int _itemsSinceLastRepeat;

    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutRepeatInstruction)
        {
            return Task.FromResult(false);
        }

        if (context.VisitedAll && _itemsSinceLastRepeat == context.AddedItems.Count)
        {
            logger.LogWarning("Repeat encountered without adding any playout items; aborting");
            throw new InvalidOperationException("Sequential playout loop detected");
        }

        _itemsSinceLastRepeat = context.AddedItems.Count;
        context.InstructionIndex = 0;
        return Task.FromResult(true);
    }
}
