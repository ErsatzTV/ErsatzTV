using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPreRollInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "pre_roll", ApplyNamingConventions = false)]
    public bool PreRoll { get; set; }

    public string Sequence { get; set; }
}
