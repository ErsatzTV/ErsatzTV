namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutDefinition
{
    public List<YamlPlayoutContentItem> Content { get; set; } = [];

    public List<YamlPlayoutSequenceItem> Sequence { get; set; } = [];

    public List<YamlPlayoutInstruction> Reset { get; set; } = [];

    public List<YamlPlayoutInstruction> Playout { get; set; } = [];
}
