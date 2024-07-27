using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public interface IYamlPlayoutHandler
{
    bool Reset { get; }

    bool Handle(YamlPlayoutContext context, YamlPlayoutInstruction instruction, ILogger<YamlPlayoutBuilder> logger);
}
