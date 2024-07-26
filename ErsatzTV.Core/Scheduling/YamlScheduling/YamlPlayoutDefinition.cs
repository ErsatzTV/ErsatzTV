namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutDefinition
{
    public List<YamlPlayoutContentItem> Content { get; set; } = [];
    public List<YamlPlayoutInstruction> Playout { get; set; } = [];
}
