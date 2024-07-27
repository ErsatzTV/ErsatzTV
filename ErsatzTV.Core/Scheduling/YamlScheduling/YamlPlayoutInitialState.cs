namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutInitialState
{
    public DateTimeOffset CurrentTime { get; set; }

    public Dictionary<string, int> ContentIndex { get; } = [];
}
