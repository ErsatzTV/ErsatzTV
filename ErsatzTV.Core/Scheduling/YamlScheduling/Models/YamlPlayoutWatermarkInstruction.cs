using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutWatermarkInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "watermark", ApplyNamingConventions = false)]
    public bool Watermark { get; set; }

    public string Name { get; set; }
}
