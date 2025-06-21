namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutSequenceInstruction : YamlPlayoutInstruction
{
    public string Sequence { get; set; }
    public int Repeat { get; set; }
}
