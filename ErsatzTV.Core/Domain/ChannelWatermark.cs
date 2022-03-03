using ErsatzTV.FFmpeg.State;

namespace ErsatzTV.Core.Domain;

public class ChannelWatermark
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ChannelWatermarkMode Mode { get; set; }
    public ChannelWatermarkImageSource ImageSource { get; set; }
    public string Image { get; set; }
    public WatermarkLocation Location { get; set; }
    public WatermarkSize Size { get; set; }
    public int WidthPercent { get; set; }
    public int HorizontalMarginPercent { get; set; }
    public int VerticalMarginPercent { get; set; }
    public int FrequencyMinutes { get; set; }
    public int DurationSeconds { get; set; }
    public int Opacity { get; set; }
}

public enum ChannelWatermarkMode
{
    None = 0,
    Permanent = 1,
    Intermittent = 2
}

public enum ChannelWatermarkImageSource
{
    Custom = 0,
    ChannelLogo = 1
}