using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class BlockItemWatermark
{
    public int BlockItemId { get; set; }
    public BlockItem BlockItem { get; set; }
    public int WatermarkId { get; set; }
    public ChannelWatermark Watermark { get; set; }
}
