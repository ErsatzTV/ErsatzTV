using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutNewEpgGroupHandler : IYamlPlayoutHandler
{
    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutNewEpgGroupInstruction)
        {
            return Task.FromResult(false);
        }

        context.GuideGroup *= -1;
        return Task.FromResult(true);
    }
}
