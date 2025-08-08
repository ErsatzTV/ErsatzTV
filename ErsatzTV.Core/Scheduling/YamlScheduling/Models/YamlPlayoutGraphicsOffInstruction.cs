using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Models;

public class YamlPlayoutGraphicsOffInstruction : YamlPlayoutInstruction
{
    [YamlMember(Alias = "graphics_off", ApplyNamingConventions = false)]
    public string GraphicsOff { get; set; }
}
