using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentItem
{
    public string Key { get; set; }
    public virtual string Order { get; set; }

    [YamlMember(Alias = "multi_part", ApplyNamingConventions = false)]
    public bool MultiPart { get; set; }
}
