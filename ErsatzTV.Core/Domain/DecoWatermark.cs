using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Domain;

public class DecoWatermark
{
    public int DecoId { get; set; }
    public Deco Deco { get; set; }
    public int WatermarkId { get; set; }
    public ChannelWatermark Watermark { get; set; }
}
