using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutSkipItemsHandler : IYamlPlayoutHandler
{
    public bool Reset => true;

    public bool Handle(YamlPlayoutContext context, YamlPlayoutInstruction instruction, ILogger<YamlPlayoutBuilder> logger)
    {
        if (instruction is not YamlPlayoutSkipItemsInstruction skipItems)
        {
            return false;
        }

        if (context.ContentIndex.TryGetValue(skipItems.Content, out int value))
        {
            value += skipItems.SkipItems;
        }
        else
        {
            value = skipItems.SkipItems;
        }

        context.ContentIndex[skipItems.Content] = value;

        return true;
    }
}
