namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutContentShowItem : YamlPlayoutContentItem
{
    public string Show { get; set; }
    public List<YamlPlayoutContentGuid> Guids { get; set; } = [];
}
