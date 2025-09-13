namespace ErsatzTV.Core.Domain;

public class PlayoutGap
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public DateTime Start { get; set; }
    public DateTime Finish { get; set; }
    public DateTimeOffset StartOffset => new DateTimeOffset(Start, TimeSpan.Zero).ToLocalTime();
    public DateTimeOffset FinishOffset => new DateTimeOffset(Finish, TimeSpan.Zero).ToLocalTime();
}
