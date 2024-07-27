using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutWaitUntilHandler : IYamlPlayoutHandler
{
    public bool Reset => true;

    public Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutWaitUntilInstruction waitUntil)
        {
            return Task.FromResult(false);
        }

        DateTimeOffset currentTime = context.CurrentTime;

        if (TimeOnly.TryParse(waitUntil.WaitUntil, out TimeOnly result))
        {
            var dayOnly = DateOnly.FromDateTime(currentTime.LocalDateTime);
            var timeOnly = TimeOnly.FromDateTime(currentTime.LocalDateTime);

            if (timeOnly > result)
            {
                if (waitUntil.Tomorrow)
                {
                    // this is wrong when offset changes
                    dayOnly = dayOnly.AddDays(1);
                    currentTime = new DateTimeOffset(dayOnly, result, currentTime.Offset);
                }
            }
            else
            {
                // this is wrong when offset changes
                currentTime = new DateTimeOffset(dayOnly, result, currentTime.Offset);
            }
        }

        context.CurrentTime = currentTime;
        return Task.FromResult(true);
    }
}
