namespace ErsatzTV.Core.Domain
{
    public class ChannelWatermark
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ChannelWatermarkMode Mode { get; set; }
        public ChannelWatermarkImageSource ImageSource { get; set; }
        public string Image { get; set; }
        public ChannelWatermarkLocation Location { get; set; }
        public ChannelWatermarkSize Size { get; set; }
        public int WidthPercent { get; set; }
        public int HorizontalMarginPercent { get; set; }
        public int VerticalMarginPercent { get; set; }
        public int FrequencyMinutes { get; set; }
        public int DurationSeconds { get; set; }
        public int Opacity { get; set; }
    }

    public enum ChannelWatermarkLocation
    {
        BottomRight = 0,
        BottomLeft = 1,
        TopRight = 2,
        TopLeft = 3,
        TopMiddle = 4,
        RightMiddle = 5,
        BottomMiddle = 6,
        LeftMiddle = 7
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

    public enum ChannelWatermarkImageSource
    {
        Custom = 0,
        ChannelLogo = 1
    }
}
