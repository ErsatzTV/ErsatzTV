namespace ErsatzTV.Core.Domain;

public class ProgramSchedule
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool KeepMultiPartEpisodesTogether { get; set; }
    public bool TreatCollectionsAsShows { get; set; }
    public bool ShuffleScheduleItems { get; set; }
    public List<ProgramScheduleItem> Items { get; set; }
    public List<Playout> Playouts { get; set; }
}
