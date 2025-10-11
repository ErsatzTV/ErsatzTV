namespace ErsatzTV.FFmpeg.State;

public record WatermarkState(
    Option<List<WatermarkFadePoint>> MaybeFadePoints,
    WatermarkLocation Location,
    WatermarkSize Size,
    double WidthPercent,
    double HorizontalMarginPercent,
    double VerticalMarginPercent,
    int Opacity,
    bool PlaceWithinSourceContent);

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
    LeftMiddle = 7,
    MiddleCenter = 8
}

public enum WatermarkSize
{
    Scaled = 0,
    ActualSize = 1
}
