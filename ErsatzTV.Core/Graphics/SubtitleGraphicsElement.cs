using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Graphics;

public class SubtitleGraphicsElement : BaseGraphicsElement
{
    [YamlMember(Alias = "z_index", ApplyNamingConventions = false)]
    public int? ZIndex { get; set; }

    [YamlMember(Alias = "epg_entries", ApplyNamingConventions = false)]
    public int EpgEntries { get; set; }

    public string Template { get; set; }
}
