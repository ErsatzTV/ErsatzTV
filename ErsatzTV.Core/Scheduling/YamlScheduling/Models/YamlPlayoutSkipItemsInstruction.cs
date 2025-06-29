using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutSkipItemsInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "skip_items", ApplyNamingConventions = false)]
    public string SkipItems { get; set; }
}
