namespace ErsatzTV.Application.Channels;

public class ChannelSortViewModel
{
    public int Id { get; set; }
    public string Number { get; set; }
    public string Name { get; set; }
    public string OriginalNumber { get; set; }
    public bool HasChanged => OriginalNumber != Number;
}
