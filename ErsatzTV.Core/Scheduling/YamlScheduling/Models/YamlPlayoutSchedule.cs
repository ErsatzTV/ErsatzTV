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
    /// Specific year(s) this schedule applies to. If empty, applies to all years.
    /// </summary>
    public List<int> Years { get; set; } = [];

    /// <summary>
    /// Whether this schedule repeats annually (default: true when no years are specified)
    /// </summary>
    public bool Recurring { get; set; } = true;

    public List<YamlPlayoutInstruction> Reset { get; set; } = [];

    public List<YamlPlayoutInstruction> Playout { get; set; } = [];
}
