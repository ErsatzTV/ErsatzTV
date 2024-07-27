using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutRepeatHandler : IYamlPlayoutHandler
{
    private int _itemsSinceLastRepeat;

    public bool Reset => false;

    public bool Handle(YamlPlayoutContext context, YamlPlayoutInstruction instruction, ILogger<YamlPlayoutBuilder> logger)
    {
        if (instruction is not YamlPlayoutRepeatInstruction)
        {
            return false;
        }

        if (_itemsSinceLastRepeat == context.Playout.Items.Count)
        {
            logger.LogWarning("Repeat encountered without adding any playout items; aborting");
            return false;
        }

        _itemsSinceLastRepeat = context.Playout.Items.Count;
        context.InstructionIndex = 0;
        return true;
    }
}
