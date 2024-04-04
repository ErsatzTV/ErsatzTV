namespace ErsatzTV.Core.Domain.Scheduling;

public class Deco
{
    public int Id { get; set; }
    public int DecoGroupId { get; set; }
    public DecoGroup DecoGroup { get; set; }

    public string Name { get; set; }

    public int? WatermarkId { get; set; }
    public ChannelWatermark Watermark { get; set; }
    
    // can be added directly to playouts
    public ICollection<Playout> Playouts { get; set; }
}
