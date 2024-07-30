using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentSmartCollectionItem : YamlPlayoutContentItem
{
    [YamlMember(Alias = "smart_collection", ApplyNamingConventions = false)]
    public string SmartCollection { get; set; }
}
