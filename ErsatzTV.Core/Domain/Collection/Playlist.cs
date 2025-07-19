namespace ErsatzTV.Core.Domain;

public class Playlist
{
    public int Id { get; set; }
    public int PlaylistGroupId { get; set; }
    public PlaylistGroup PlaylistGroup { get; set; }
    public string Name { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<PlaylistItem> Items { get; set; }
    //public DateTime DateUpdated { get; set; }
}
