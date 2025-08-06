using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.FFmpeg.State;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming;

public class WatermarkElement : IGraphicsElement, IDisposable
{
    private readonly string _imagePath;
    private readonly ChannelWatermark _watermark;
    private readonly List<Image> _scaledFrames = [];
    private readonly List<double> _frameDelays = [];

    private double _animatedDurationSeconds;
    private Image _sourceImage;
    private Point _location;

    public WatermarkElement(WatermarkOptions watermarkOptions)
    {
        // TODO: better model coming in here?
        foreach (var imagePath in watermarkOptions.ImagePath)
        {
            _imagePath = imagePath;
        }

        foreach (var watermark in watermarkOptions.Watermark)
        {
            _watermark = watermark;
        }
    }

    public bool IsValid => _imagePath != null && _watermark != null;

    public async Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken)
    {
        _sourceImage = await Image.LoadAsync(_imagePath, cancellationToken);

        int scaledWidth = _sourceImage.Width;
        int scaledHeight = _sourceImage.Height;
        if (_watermark.Size == WatermarkSize.Scaled)
        {
            scaledWidth = (int)Math.Round(_watermark.WidthPercent / 100.0 * frameSize.Width);
            double aspectRatio = (double)_sourceImage.Height / _sourceImage.Width;
            scaledHeight = (int)(scaledWidth * aspectRatio);
        }

        int horizontalMargin = (int)Math.Round(_watermark.HorizontalMarginPercent / 100.0 * frameSize.Width);
        int verticalMargin = (int)Math.Round(_watermark.VerticalMarginPercent / 100.0 * frameSize.Height);

        _location = CalculatePosition(
            _watermark.Location,
            frameSize.Width,
            frameSize.Height,
            scaledWidth,
            scaledHeight,
            horizontalMargin,
            verticalMargin);

        float opacity = _watermark.Opacity / 100.0f;
        _animatedDurationSeconds = 0;

        for (int i = 0; i < _sourceImage.Frames.Count; i++)
        {
            var frame = _sourceImage.Frames.CloneFrame(i);
            frame.Mutate(ctx => ctx.Resize(scaledWidth, scaledHeight).Opacity(opacity));
            _scaledFrames.Add(frame);

            var frameDelay = _sourceImage.Frames[i].Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay / 100.0;
            _animatedDurationSeconds += frameDelay;
            _frameDelays.Add(frameDelay);
        }
    }

    public void Draw(object context, TimeSpan timestamp)
    {
        if (context is not IImageProcessingContext imageProcessingContext)
        {
            return;
        }

        Image frameForTimestamp = GetFrameForTimestamp(timestamp);

        // scaled frames already have opacity set
        imageProcessingContext.DrawImage(frameForTimestamp, _location, 1f);
    }

    private Image GetFrameForTimestamp(TimeSpan timestamp)
    {
        if (_scaledFrames.Count <= 1)
        {
            return _scaledFrames[0];
        }

        double currentTime = timestamp.TotalSeconds % _animatedDurationSeconds;

        double frameTime = 0;
        for (int i = 0; i < _sourceImage.Frames.Count; i++)
        {
            frameTime += _frameDelays[i];
            if (currentTime <= frameTime)
            {
                return _scaledFrames[i];
            }
        }

        return _scaledFrames.Last();
    }

    private static Point CalculatePosition(
        WatermarkLocation location,
        int frameWidth,
        int frameHeight,
        int scaledWidth,
        int scaledHeight,
        int horizontalMargin,
        int verticalMargin)
    {
        // TODO: source content margins

        return location switch
        {
            WatermarkLocation.BottomLeft => new Point(horizontalMargin, frameHeight - scaledHeight - verticalMargin),
            WatermarkLocation.TopLeft => new Point(horizontalMargin, verticalMargin),
            WatermarkLocation.TopRight => new Point(frameWidth - scaledWidth - horizontalMargin, verticalMargin),
            WatermarkLocation.TopMiddle => new Point((frameWidth - scaledWidth) / 2, verticalMargin),
            WatermarkLocation.RightMiddle => new Point(
                frameWidth - scaledWidth - horizontalMargin,
                (frameHeight - scaledHeight) / 2),
            WatermarkLocation.BottomMiddle => new Point(
                (frameWidth - scaledWidth) / 2,
                frameHeight - scaledHeight - verticalMargin),
            WatermarkLocation.LeftMiddle => new Point(horizontalMargin, (frameHeight - scaledHeight) / 2),
            _ => new Point(
                frameWidth - scaledWidth - horizontalMargin,
                frameHeight - scaledHeight - verticalMargin),
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sourceImage?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }
}