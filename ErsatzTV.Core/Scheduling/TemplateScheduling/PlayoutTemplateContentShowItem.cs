namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateContentShowItem : PlayoutTemplateContentItem
{
    public string Show { get; set; }
    public List<PlayoutTemplateContentGuid> Guids { get; set; } = [];
}
