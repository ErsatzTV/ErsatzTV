namespace ErsatzTV.Core.Domain;

public class JellyfinConnection
{
    public int Id { get; set; }
    public string Address { get; set; }
    public int JellyfinMediaSourceId { get; set; }
    public JellyfinMediaSource JellyfinMediaSource { get; set; }
}