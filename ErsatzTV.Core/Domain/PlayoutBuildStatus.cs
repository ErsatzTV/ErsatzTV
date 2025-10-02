namespace ErsatzTV.Core.Domain;

public class PlayoutBuildStatus
{
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public DateTimeOffset LastBuild { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
}
