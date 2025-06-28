using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutPadUntilInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "pad_until", ApplyNamingConventions = false)]
    public string PadUntil { get; set; }

    public bool Tomorrow { get; set; }

    public bool Trim { get; set; }

    public string Fallback { get; set; }

    [YamlMember(Alias = "discard_attempts", ApplyNamingConventions = false)]
    public int DiscardAttempts { get; set; }

    [YamlMember(Alias = "stop_before_end", ApplyNamingConventions = false)]
    public bool StopBeforeEnd { get; set; } = true;
}
