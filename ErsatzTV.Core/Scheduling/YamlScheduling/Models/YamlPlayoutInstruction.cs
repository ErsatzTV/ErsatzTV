using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutInstruction
{
    public virtual bool ChangesIndex => false;

    public string Content { get; set; }

    [YamlMember(Alias = "filler_kind", ApplyNamingConventions = false)]
    public string FillerKind { get; set; }

    [YamlIgnore]
    public string SequenceKey { get; set; }
}
