using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutEpgGroupInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "epg_group", ApplyNamingConventions = false)]
    public bool EpgGroup { get; set; }

    public bool? Advance { get; set; }
}
