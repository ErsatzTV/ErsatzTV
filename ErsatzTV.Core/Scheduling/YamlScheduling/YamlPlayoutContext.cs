using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout, YamlPlayoutDefinition definition)
{
    private readonly System.Collections.Generic.HashSet<int> _visitedInstructions = [];
    private int _instructionIndex;

    public Playout Playout { get; } = playout;

    public YamlPlayoutDefinition Definition { get; } = definition;

    public DateTimeOffset CurrentTime { get; set; }

    public int InstructionIndex
    {
        get => _instructionIndex;
        set
        {
            _instructionIndex = value;
            _visitedInstructions.Add(value);
        }
    }

    public bool VisitedAll => _visitedInstructions.Count >= Definition.Playout.Count;

    public int GuideGroup { get; set; }
}
