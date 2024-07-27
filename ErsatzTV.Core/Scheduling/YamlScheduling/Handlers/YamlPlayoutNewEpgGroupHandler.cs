using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutNewEpgGroupHandler : IYamlPlayoutHandler
{
    public bool Handle(YamlPlayoutContext context, YamlPlayoutInstruction instruction, ILogger<YamlPlayoutBuilder> logger)
    {
        if (instruction is not YamlPlayoutNewEpgGroupInstruction)
        {
            return false;
        }

        context.GuideGroup *= -1;
        context.InstructionIndex++;
        return true;
    }
}
