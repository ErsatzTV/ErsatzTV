namespace ErsatzTV.Core.Domain;

public class PlayoutItemWatermark
{
    public int PlayoutItemId { get; set; }
    public PlayoutItem PlayoutItem { get; set; }
    public ChannelWatermark Watermark { get; set; }
    public int? WatermarkId { get; set; }
}
