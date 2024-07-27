using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutInstruction
{
    public string Content { get; set; }

    [YamlMember(Alias = "filler_kind", ApplyNamingConventions = false)]
    public string FillerKind { get; set; }
}
