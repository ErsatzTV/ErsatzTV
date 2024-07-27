using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutNewEpgGroupInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "new_epg_group", ApplyNamingConventions = false)]
    public string NewEpgGroup { get; set; }
}
