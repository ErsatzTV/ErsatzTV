namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutRepeatInstruction : YamlPlayoutInstruction
{
    public override bool ChangesIndex => true;

    public bool Repeat { get; set; }
}
