using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutGraphicsOnInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "graphics_on", ApplyNamingConventions = false)]
    public string GraphicsOn { get; set; }
}
