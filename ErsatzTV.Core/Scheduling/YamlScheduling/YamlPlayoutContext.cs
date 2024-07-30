using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout, YamlPlayoutDefinition definition, int guideGroup)
{
    private readonly System.Collections.Generic.HashSet<int> _visitedInstructions = [];
    private int _instructionIndex;
    private bool _guideGroupLocked;

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

    public int NextGuideGroup()
    {
        if (_guideGroupLocked)
        {
            return guideGroup;
        }

        guideGroup++;
        if (guideGroup > 1000)
        {
            guideGroup = 1;
        }

        return guideGroup;
    }

    public void LockGuideGroup()
    {
        NextGuideGroup();
        _guideGroupLocked = true;
    }

    public void UnlockGuideGroup()
    {
        _guideGroupLocked = false;
    }
}
