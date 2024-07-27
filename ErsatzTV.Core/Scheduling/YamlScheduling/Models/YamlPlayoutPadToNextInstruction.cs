using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutPadToNextInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "pad_to_next", ApplyNamingConventions = false)]
    public int PadToNext { get; set; }

    public bool Trim { get; set; }

    public string Fallback { get; set; }

    [YamlMember(Alias = "discard_attempts", ApplyNamingConventions = false)]
    public int DiscardAttempts { get; set; }
}
