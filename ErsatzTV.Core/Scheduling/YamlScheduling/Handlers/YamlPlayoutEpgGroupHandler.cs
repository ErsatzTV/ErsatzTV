using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutEpgGroupHandler : IYamlPlayoutHandler
{
    public bool Reset => false;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutEpgGroupInstruction epgGroup)
        {
            return Task.FromResult(false);
        }

        if (epgGroup.EpgGroup)
        {
            context.LockGuideGroup();
        }
        else
        {
            context.UnlockGuideGroup();
        }

        return Task.FromResult(true);
    }
}
