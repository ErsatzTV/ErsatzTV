using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutRewindHandler : IYamlPlayoutHandler
{
    public bool Reset => true;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutRewindInstruction rewind)
        {
            return Task.FromResult(false);
        }

        if (TimeSpan.TryParse(rewind.Rewind ?? string.Empty, out TimeSpan amount))
        {
            context.CurrentTime -= amount;
        }

        return Task.FromResult(true);
    }
}