using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout, YamlPlayoutDefinition definition, int guideGroup)
{
    private readonly System.Collections.Generic.HashSet<int> _visitedInstructions = [];
    private int _instructionIndex;
    private bool _guideGroupLocked;
    private int _guideGroup = guideGroup;

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

    public int PeekNextGuideGroup()
    {
        if (_guideGroupLocked)
        {
            return _guideGroup;
        }

        int result = _guideGroup + 1;
        if (result > 1000)
        {
            result = 1;
        }

        return result;
    }

    public void AdvanceGuideGroup()
    {
        if (_guideGroupLocked)
        {
            return;
        }

        _guideGroup++;
        if (_guideGroup > 1000)
        {
            _guideGroup = 1;
        }
    }

    public void LockGuideGroup(bool advance = true)
    {
        if (advance)
        {
            AdvanceGuideGroup();
        }

        _guideGroupLocked = true;
    }

    public void UnlockGuideGroup()
    {
        _guideGroupLocked = false;
    }
}
