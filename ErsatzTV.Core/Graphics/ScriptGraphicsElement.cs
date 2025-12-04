using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class ScriptGraphicsElement : BaseGraphicsElement
{
    [YamlMember(Alias = "command", ApplyNamingConventions = false)]
    public string Command { get; set; }

    [YamlMember(Alias = "args", ApplyNamingConventions = false)]
    public List<string> Arguments { get; set; }

    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    [YamlMember(Alias = "start_seconds", ApplyNamingConventions = false)]
    public double? StartSeconds { get; set; }

    [YamlMember(Alias = "duration_seconds", ApplyNamingConventions = false)]
    public double? DurationSeconds { get; set; }

    [YamlMember(Alias = "pixel_format", ApplyNamingConventions = false)]
    public string PixelFormat { get; set; }

    [YamlMember(Alias = "format", ApplyNamingConventions = false)]
    public ScriptGraphicsFormat Format { get; set; }
}
