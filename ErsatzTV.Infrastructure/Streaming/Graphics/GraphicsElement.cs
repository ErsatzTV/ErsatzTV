using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.FFmpeg.State;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public abstract class GraphicsElement : IGraphicsElement
{
    public abstract int ZIndex { get; }

    public bool IsFinished { get; set; }

    public abstract Task InitializeAsync(GraphicsEngineContext context, CancellationToken cancellationToken);

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
        int verticalMargin) =>
        location switch
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
            WatermarkLocation.MiddleCenter => new SKPointI(
                (frameWidth - imageWidth) / 2 + horizontalMargin,
                (frameHeight - imageHeight) / 2 + verticalMargin),
            _ => new SKPointI(
                frameWidth - imageWidth - horizontalMargin,
                frameHeight - imageHeight - verticalMargin)
        };

    protected static WatermarkMargins NormalMargins(
        Resolution frameSize,
        double horizontalMarginPercent,
        double verticalMarginPercent)
    {
        double horizontalMargin = Math.Round(horizontalMarginPercent / 100.0 * frameSize.Width);
        double verticalMargin = Math.Round(verticalMarginPercent / 100.0 * frameSize.Height);

        return new WatermarkMargins((int)Math.Round(horizontalMargin), (int)Math.Round(verticalMargin));
    }

    protected static WatermarkMargins SourceContentMargins(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        double horizontalMarginPercent,
        double verticalMarginPercent)
    {
        int horizontalPadding = frameSize.Width - squarePixelFrameSize.Width;
        int verticalPadding = frameSize.Height - squarePixelFrameSize.Height;

        double horizontalMargin = Math.Round(
            horizontalMarginPercent / 100.0 * squarePixelFrameSize.Width
            + horizontalPadding / 2.0);
        double verticalMargin = Math.Round(
            verticalMarginPercent / 100.0 * squarePixelFrameSize.Height
            + verticalPadding / 2.0);

        return new WatermarkMargins((int)Math.Round(horizontalMargin), (int)Math.Round(verticalMargin));
    }

    protected sealed record WatermarkMargins(int HorizontalMargin, int VerticalMargin);
}
