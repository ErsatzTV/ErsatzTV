namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutSequenceItem
{
    public string Key { get; set; }

    public List<YamlPlayoutInstruction> Items { get; set; } = [];
}
