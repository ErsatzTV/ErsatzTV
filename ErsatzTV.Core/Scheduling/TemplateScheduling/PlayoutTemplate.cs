namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplate
{
    public List<PlayoutTemplateContentItem> Content { get; set; } = [];
    public List<PlayoutTemplateItem> Playout { get; set; } = [];
}
