using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutDurationInstruction : YamlPlayoutInstruction
{
    public string Duration { get; set; }

    public bool Trim { get; set; }

    public string Fallback { get; set; }

    [YamlMember(Alias = "discard_attempts", ApplyNamingConventions = false)]
    public int DiscardAttempts { get; set; }
}
