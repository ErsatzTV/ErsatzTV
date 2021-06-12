namespace ErsatzTV.Core.Domain
{
    public class ChannelWatermark
    {
        public int Id { get; set; }
        public Channel Channel { get; set; }
        public ChannelWatermarkLocation Location { get; set; }
        public ChannelWatermarkSize Size { get; set; }
        public ChannelWatermarkMode Mode { get; set; }
        public int WidthPercent { get; set; }
        public int HorizontalMarginPercent { get; set; }
        public int VerticalMarginPercent { get; set; }
        public int FrequencyMinutes { get; set; }
        public int DurationSeconds { get; set; }
    }

    public enum ChannelWatermarkLocation
    {
        BottomRight = 0,
        BottomLeft = 1,
        TopRight = 2,
        TopLeft = 3
    }

    public enum ChannelWatermarkSize
    {
        Scaled = 0,
        ActualSize = 1
    }

    public enum ChannelWatermarkMode
    {
        None = 0,
        Permanent = 1,
        Intermittent = 2
    }
}
