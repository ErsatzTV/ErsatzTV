namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutContentShowItem : YamlPlayoutContentItem
{
    public string Show { get; set; }
    public List<YamlPlayoutContentGuid> Guids { get; set; } = [];
}
