namespace ErsatzTV.Core.Domain.Scheduling;

public class PlayoutHistory
{
    public int Id { get; set; }
    
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    
    public int BlockId { get; set; }
    public Block Block { get; set; }
    public PlaybackOrder PlaybackOrder { get; set; }
    public int Index { get; set; }
    
    // something that uniquely identifies the collection within the block 
    public string Key { get; set; }
    
    // last occurence of an item from this collection in the playout
    public DateTime When { get; set; }

    // details about the item
    public string Details { get; set; }
}
