using LanguageExt;

namespace ErsatzTV.FFmpeg.State;

public record WatermarkState(
    Option<List<WatermarkFadePoint>> MaybeFadePoints,
    WatermarkLocation Location,
    WatermarkSize Size,
    int WidthPercent,
    int HorizontalMarginPercent,
    int VerticalMarginPercent,
    int Opacity);

public record WatermarkFadePoint(TimeSpan Time, TimeSpan EnableStart, TimeSpan EnableFinish);

public record WatermarkFadeIn(TimeSpan Time, TimeSpan EnableStart, TimeSpan EnableFinish) : WatermarkFadePoint(
    Time,
    EnableStart,
    EnableFinish);

public record WatermarkFadeOut(TimeSpan Time, TimeSpan EnableStart, TimeSpan EnableFinish) : WatermarkFadePoint(
    Time,
    EnableStart,
    EnableFinish);

public enum WatermarkLocation
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

public enum WatermarkSize
{
    Scaled = 0,
    ActualSize = 1
}