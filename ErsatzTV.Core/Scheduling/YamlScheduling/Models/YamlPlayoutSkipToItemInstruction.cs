using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutSkipToItemInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "skip_to_item", ApplyNamingConventions = false)]
    public string SkipToItem { get; set; }

    public int Season { get; set; }
    public int Episode { get; set; }
}
