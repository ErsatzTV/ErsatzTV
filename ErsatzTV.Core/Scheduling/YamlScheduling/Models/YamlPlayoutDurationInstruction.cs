using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutDurationInstruction : YamlPlayoutInstruction
{
    public string Duration { get; set; }

    public bool Trim { get; set; }

    [YamlMember(Alias = "offline_tail", ApplyNamingConventions = false)]
    public bool OfflineTail { get; set; }

    public string Fallback { get; set; }

    [YamlMember(Alias = "discard_attempts", ApplyNamingConventions = false)]
    public int DiscardAttempts { get; set; }

    [YamlMember(Alias = "stop_before_end", ApplyNamingConventions = false)]
    public bool StopBeforeEnd { get; set; } = true;
}
