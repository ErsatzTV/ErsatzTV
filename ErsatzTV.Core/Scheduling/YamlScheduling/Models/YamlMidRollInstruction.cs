using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlMidRollInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "mid_roll", ApplyNamingConventions = false)]
    public bool MidRoll { get; set; }

    public string Sequence { get; set; }

    public string Expression { get; set; } = "true";
}
