using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout)
{
    public Playout Playout { get; } = playout;

    public DateTimeOffset CurrentTime { get; set; }

    public int InstructionIndex { get; set; }

    public int GuideGroup { get; set; }

    // only used for initial state (skip items)
    public Dictionary<string, int> ContentIndex { get; } = [];
}
