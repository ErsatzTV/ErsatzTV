using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutSchedule
{
    public string Name { get; set; }

    [YamlMember(Alias = "start_date", ApplyNamingConventions = false)]
    public string StartDate { get; set; }

    [YamlMember(Alias = "end_date", ApplyNamingConventions = false)]
    public string EndDate { get; set; }

    /// <summary>
    /// Priority for schedule matching. Higher values are checked first. Default is 0.
    /// </summary>
    public int Priority { get; set; }

    public List<YamlPlayoutInstruction> Reset { get; set; } = [];

    public List<YamlPlayoutInstruction> Playout { get; set; } = [];
}
