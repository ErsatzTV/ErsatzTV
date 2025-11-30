namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutDefinition
{
    public List<string> Import { get; set; } = [];

    public List<YamlPlayoutContentItem> Content { get; set; } = [];

    public List<YamlPlayoutSequenceItem> Sequence { get; set; } = [];

    /// <summary>
    /// Scheduled playouts that apply to specific date ranges
    /// </summary>
    public List<YamlPlayoutSchedule> Schedules { get; set; } = [];

    /// <summary>
    /// Default reset instructions (used when no schedule matches)
    /// </summary>
    public List<YamlPlayoutInstruction> Reset { get; set; } = [];

    /// <summary>
    /// Default playout instructions (used when no schedule matches)
    /// </summary>
    public List<YamlPlayoutInstruction> Playout { get; set; } = [];
}
