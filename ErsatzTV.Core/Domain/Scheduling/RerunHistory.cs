namespace ErsatzTV.Core.Domain.Scheduling;

public class RerunHistory
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public int RerunCollectionId { get; set; }
    public RerunCollection RerunCollection { get; set; }
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public DateTime When { get; set; }
}
