using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutShuffleSequenceInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "shuffle_sequence", ApplyNamingConventions = false)]
    public string ShuffleSequence { get; set; }
}
