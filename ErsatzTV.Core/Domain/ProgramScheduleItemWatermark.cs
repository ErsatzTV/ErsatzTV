namespace ErsatzTV.Core.Domain;

public class ProgramScheduleItemWatermark
{
    public int ProgramScheduleItemId { get; set; }
    public ProgramScheduleItem ProgramScheduleItem { get; set; }
    public int WatermarkId { get; set; }
    public ChannelWatermark Watermark { get; set; }
}
