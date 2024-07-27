using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutWaitUntilHandler : IYamlPlayoutHandler
{
    public bool Reset => true;

    public bool Handle(YamlPlayoutContext context, YamlPlayoutInstruction instruction, ILogger<YamlPlayoutBuilder> logger)
    {
        if (instruction is not YamlPlayoutWaitUntilInstruction waitUntil)
        {
            return false;
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
        context.InstructionIndex++;
        return true;
    }
}
