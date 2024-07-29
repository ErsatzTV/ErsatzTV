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
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutRepeatInstruction)
        {
            return Task.FromResult(false);
        }

        if (context.VisitedAll && _itemsSinceLastRepeat == context.Playout.Items.Count)
        {
            logger.LogWarning("Repeat encountered without adding any playout items; aborting");
            throw new InvalidOperationException("YAML playout loop detected");
        }

        _itemsSinceLastRepeat = context.Playout.Items.Count;
        context.InstructionIndex = 0;
        return Task.FromResult(true);
    }
}
