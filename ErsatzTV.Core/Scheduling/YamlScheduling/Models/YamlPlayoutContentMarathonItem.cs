using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentMarathonItem : YamlPlayoutContentItem
{
    public string Marathon { get; set; }

    public List<YamlPlayoutContentGuid> Guids { get; set; } = [];

    public List<string> Searches { get; set; } = [];

    [YamlMember(Alias = "group_by", ApplyNamingConventions = false)]
    public string GroupBy { get; set; }

    [YamlMember(Alias = "shuffle_groups", ApplyNamingConventions = false)]
    public bool ShuffleGroups { get; set; }

    [YamlMember(Alias = "item_order", ApplyNamingConventions = false)]
    public string ItemOrder { get; set; }

    [YamlMember(Alias = "play_all_items", ApplyNamingConventions = false)]
    public bool PlayAllItems { get; set; }

    public override string Order
    {
        get => "none";
        set => throw new NotSupportedException();
    }
}
