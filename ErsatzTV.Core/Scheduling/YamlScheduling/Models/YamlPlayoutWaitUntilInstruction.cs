using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutWaitUntilInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "wait_until", ApplyNamingConventions = false)]
    public string WaitUntil { get; set; }

    public bool Tomorrow { get; set; }

    [YamlMember(Alias = "rewind_on_reset", ApplyNamingConventions = false)]
    public bool RewindOnReset { get; set; }
}
