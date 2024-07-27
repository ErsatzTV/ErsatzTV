using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout, YamlPlayoutDefinition definition)
{
    public Playout Playout { get; } = playout;

    public YamlPlayoutDefinition Definition { get; } = definition;

    public DateTimeOffset CurrentTime { get; set; }

    public int InstructionIndex { get; set; }

    public int GuideGroup { get; set; }

    // only used for initial state (skip items)
    public Dictionary<string, int> ContentIndex { get; } = [];
}
