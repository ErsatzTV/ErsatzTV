namespace ErsatzTV.Core.Metadata;

public class EpgProgrammeTemplateData
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset Stop { get; set; }
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Description { get; set; }
    public string Rating { get; set; }
    public string[] Categories { get; set; }
    public string Date { get; set; }
}
