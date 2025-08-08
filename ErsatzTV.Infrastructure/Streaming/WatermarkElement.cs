using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using NCalc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming;

public class WatermarkElement : IGraphicsElement, IDisposable
{
    private readonly ILogger _logger;
    private readonly string _imagePath;
    private readonly ChannelWatermark _watermark;
    private readonly List<Image> _scaledFrames = [];
    private readonly List<double> _frameDelays = [];

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private double _animatedDurationSeconds;
    private Image _sourceImage;
    private Point _location;

    public WatermarkElement(WatermarkOptions watermarkOptions, ILogger logger)
    {
        _logger = logger;
        // TODO: better model coming in here?
        foreach (var imagePath in watermarkOptions.ImagePath)
        {
            _imagePath = imagePath;
        }

        foreach (var watermark in watermarkOptions.Watermark)
        {
            _watermark = watermark;
            ZIndex = watermark.ZIndex;
        }
    }

    public bool IsValid => _imagePath != null && _watermark != null;

    public int ZIndex { get; }

    public bool IsFailed { get; set; }

    public async Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken)
    {
        try
        {
            if (_watermark.Mode is ChannelWatermarkMode.Intermittent)
            {
                string expressionString = $@"
                    if(time_of_day_seconds % {_watermark.FrequencyMinutes * 60} < 1,
                        (time_of_day_seconds % {_watermark.FrequencyMinutes * 60}),
                        if(time_of_day_seconds % {_watermark.FrequencyMinutes * 60} < {1 + _watermark.DurationSeconds},
                            1,
                            if(time_of_day_seconds % {_watermark.FrequencyMinutes * 60} < {1 + _watermark.DurationSeconds + 1},
                                1 - ((time_of_day_seconds % {_watermark.FrequencyMinutes * 60} - {1 + _watermark.DurationSeconds}) / 1),
                                0
                            )
                        )
                    )";
                _maybeOpacityExpression = new Expression(expressionString);
            }
            else if (_watermark.Mode is ChannelWatermarkMode.OpacityExpression && !string.IsNullOrWhiteSpace(_watermark.OpacityExpression))
            {
                _maybeOpacityExpression = new Expression(_watermark.OpacityExpression);
            }
            else
            {
                _opacity = _watermark.Opacity / 100.0f;
            }

            foreach (var expression in _maybeOpacityExpression)
            {
                expression.EvaluateFunction += OpacityExpressionHelper.EvaluateFunction;
            }

            bool isRemoteUri = Uri.TryCreate(_imagePath, UriKind.Absolute, out var uriResult)
                               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isRemoteUri)
            {
                using var client = new HttpClient();
                await using Stream imageStream = await client.GetStreamAsync(uriResult, cancellationToken);
                _sourceImage = await Image.LoadAsync(imageStream, cancellationToken);
            }
            else
            {
                _sourceImage = await Image.LoadAsync(_imagePath!, cancellationToken);
            }

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

            _animatedDurationSeconds = 0;

            for (int i = 0; i < _sourceImage.Frames.Count; i++)
            {
                var frame = _sourceImage.Frames.CloneFrame(i);
                frame.Mutate(ctx => ctx.Resize(scaledWidth, scaledHeight));
                _scaledFrames.Add(frame);

                var frameDelay = _sourceImage.Frames[i].Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay / 100.0;
                _animatedDurationSeconds += frameDelay;
                _frameDelays.Add(frameDelay);
            }
        }
        catch (Exception ex)
        {
            IsFailed = true;
            _logger.LogWarning(ex, "Failed to initialize watermark element; will disable for this content");
        }
    }

    public ValueTask<Option<PreparedElementImage>> PrepareImage(
        TimeSpan timeOfDay,
        TimeSpan contentTime,
        TimeSpan contentTotalTime,
        TimeSpan channelTime,
        CancellationToken cancellationToken)
    {
        float opacity = _opacity;
        foreach (var expression in _maybeOpacityExpression)
        {
            opacity = OpacityExpressionHelper.GetOpacity(
                expression,
                timeOfDay,
                contentTime,
                contentTotalTime,
                channelTime);
        }

        if (opacity == 0)
        {
            return ValueTask.FromResult(Option<PreparedElementImage>.None);
        }

        Image frameForTimestamp = GetFrameForTimestamp(contentTime);
        return ValueTask.FromResult(Optional(new PreparedElementImage(frameForTimestamp, _location, opacity, false)));
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

    internal static Point CalculatePosition(
        WatermarkLocation location,
        int frameWidth,
        int frameHeight,
        int imageWidth,
        int imageHeight,
        int horizontalMargin,
        int verticalMargin)
    {
        // TODO: source content margins

        return location switch
        {
            WatermarkLocation.BottomLeft => new Point(horizontalMargin, frameHeight - imageHeight - verticalMargin),
            WatermarkLocation.TopLeft => new Point(horizontalMargin, verticalMargin),
            WatermarkLocation.TopRight => new Point(frameWidth - imageWidth - horizontalMargin, verticalMargin),
            WatermarkLocation.TopMiddle => new Point((frameWidth - imageWidth) / 2, verticalMargin),
            WatermarkLocation.RightMiddle => new Point(
                frameWidth - imageWidth - horizontalMargin,
                (frameHeight - imageHeight) / 2),
            WatermarkLocation.BottomMiddle => new Point(
                (frameWidth - imageWidth) / 2,
                frameHeight - imageHeight - verticalMargin),
            WatermarkLocation.LeftMiddle => new Point(horizontalMargin, (frameHeight - imageHeight) / 2),
            _ => new Point(
                frameWidth - imageWidth - horizontalMargin,
                frameHeight - imageHeight - verticalMargin),
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sourceImage?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }
}