using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContext(Playout playout, YamlPlayoutDefinition definition, int guideGroup)
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly System.Collections.Generic.HashSet<int> _visitedInstructions = [];
    private readonly Stack<FillerKind> _fillerKind = new();
    private int _guideGroup = guideGroup;
    private bool _guideGroupLocked;
    private int _instructionIndex;
    private Option<int> _channelWatermarkId;
    private Option<string> _preRollSequence;
    private Option<string> _postRollSequence;

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

    public void UnlockGuideGroup() => _guideGroupLocked = false;

    public void SetChannelWatermarkId(int id)
    {
        _channelWatermarkId = id;
    }

    public void ClearChannelWatermarkId()
    {
        _channelWatermarkId = Option<int>.None;
    }

    public Option<int> GetChannelWatermarkId() => _channelWatermarkId;

    public void SetPreRollSequence(string sequence)
    {
        _preRollSequence = sequence;
    }

    public void ClearPreRollSequence()
    {
        _preRollSequence = Option<string>.None;
    }

    public Option<string> GetPreRollSequence() => _preRollSequence;

    public void SetPostRollSequence(string sequence)
    {
        _postRollSequence = sequence;
    }

    public void ClearPostRollSequence()
    {
        _postRollSequence = Option<string>.None;
    }

    public Option<string> GetPostRollSequence() => _postRollSequence;

    public void PushFillerKind(FillerKind fillerKind)
    {
        _fillerKind.Push(fillerKind);
    }

    public void PopFillerKind() => _fillerKind.Pop();

    public Option<FillerKind> GetFillerKind() =>
        _fillerKind.TryPeek(out FillerKind fillerKind) ? fillerKind : Option<FillerKind>.None;

    public string Serialize()
    {
        int? channelWatermarkId = null;
        foreach (int id in _channelWatermarkId)
        {
            channelWatermarkId = id;
        }

        string preRollSequence = null;
        foreach (string sequence in _preRollSequence)
        {
            preRollSequence = sequence;
        }

        var state = new State(_instructionIndex, _guideGroup, _guideGroupLocked, channelWatermarkId, preRollSequence);
        return JsonConvert.SerializeObject(state, Formatting.None, JsonSettings);
    }

    public void Reset(PlayoutAnchor anchor, DateTimeOffset start)
    {
        CurrentTime = new DateTimeOffset(anchor.NextStart.ToLocalTime(), start.Offset);

        if (string.IsNullOrWhiteSpace(anchor.Context))
        {
            return;
        }

        State state = JsonConvert.DeserializeObject<State>(anchor.Context);

        foreach (int instructionIndex in Optional(state.InstructionIndex))
        {
            _instructionIndex = instructionIndex;
        }

        foreach (int guideGroup in Optional(state.GuideGroup))
        {
            _guideGroup = guideGroup;
        }

        foreach (bool guideGroupLocked in Optional(state.GuideGroupLocked))
        {
            _guideGroupLocked = guideGroupLocked;
        }

        foreach (int channelWatermarkId in Optional(state.ChannelWatermarkId))
        {
            _channelWatermarkId = channelWatermarkId;
        }

        foreach (string preRollSequence in Optional(state.PreRollSequence))
        {
            _preRollSequence = preRollSequence;
        }
    }

    public record State(
        int? InstructionIndex,
        int? GuideGroup,
        bool? GuideGroupLocked,
        int? ChannelWatermarkId,
        string PreRollSequence);
}
