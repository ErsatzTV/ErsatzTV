using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentMultiCollectionItem : YamlPlayoutContentItem
{
    [YamlMember(Alias = "multi_collection", ApplyNamingConventions = false)]
    public string MultiCollection { get; set; }
}
