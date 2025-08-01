using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPostRollInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "post_roll", ApplyNamingConventions = false)]
    public bool PostRoll { get; set; }

    public string Sequence { get; set; }
}
