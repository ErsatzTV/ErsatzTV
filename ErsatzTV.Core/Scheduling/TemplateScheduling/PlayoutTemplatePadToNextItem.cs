using YamlDotNet.Serialization;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplatePadToNextItem : PlayoutTemplateItem
{
    [YamlMember(Alias = "pad_to_next", ApplyNamingConventions = false)]
    public int PadToNext { get; set; }
}
