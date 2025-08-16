using ErsatzTV.Core.Domain;
using ErsatzTV.FFmpeg.State;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public abstract class GraphicsElement : IGraphicsElement
{
    public int ZIndex { get; protected set; }

    public bool IsFailed { get; set; }

    public abstract Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken);

    public abstract ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken);

    protected static SKPointI CalculatePosition(
        WatermarkLocation location,
        int frameWidth,
        int frameHeight,
        int imageWidth,
        int imageHeight,
        int horizontalMargin,
        int verticalMargin)
    {
        return location switch
        {
            WatermarkLocation.BottomLeft => new SKPointI(horizontalMargin, frameHeight - imageHeight - verticalMargin),
            WatermarkLocation.TopLeft => new SKPointI(horizontalMargin, verticalMargin),
            WatermarkLocation.TopRight => new SKPointI(frameWidth - imageWidth - horizontalMargin, verticalMargin),
            WatermarkLocation.TopMiddle => new SKPointI((frameWidth - imageWidth) / 2, verticalMargin),
            WatermarkLocation.RightMiddle => new SKPointI(
                frameWidth - imageWidth - horizontalMargin,
                (frameHeight - imageHeight) / 2),
            WatermarkLocation.BottomMiddle => new SKPointI(
                (frameWidth - imageWidth) / 2,
                frameHeight - imageHeight - verticalMargin),
            WatermarkLocation.LeftMiddle => new SKPointI(horizontalMargin, (frameHeight - imageHeight) / 2),
            _ => new SKPointI(
                frameWidth - imageWidth - horizontalMargin,
                frameHeight - imageHeight - verticalMargin),
        };
    }
}
