using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using NCalc;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class WatermarkElement : IGraphicsElement, IDisposable
{
    private readonly ILogger _logger;
    private readonly string _imagePath;
    private readonly ChannelWatermark _watermark;
    private readonly List<SKBitmap> _scaledFrames = [];
    private readonly List<int> _frameDelays = [];

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private int _animatedDurationMs;
    private SKCodec _sourceCodec;
    private SKPointI _location;

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

    public async Task InitializeAsync(Resolution squarePixelFrameSize, Resolution frameSize, int frameRate, CancellationToken cancellationToken)
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

            Stream imageStream;
            bool isRemoteUri = Uri.TryCreate(_imagePath, UriKind.Absolute, out var uriResult)
                               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isRemoteUri)
            {
                using var client = new HttpClient();
                imageStream = new MemoryStream(await client.GetByteArrayAsync(uriResult, cancellationToken));
            }
            else
            {
                imageStream = new FileStream(_imagePath!, FileMode.Open, FileAccess.Read);
            }

            _sourceCodec = SKCodec.Create(imageStream);

            int sourceWidth = _sourceCodec.Info.Width;
            int sourceHeight = _sourceCodec.Info.Height;

            int scaledWidth = sourceWidth;
            int scaledHeight = sourceHeight;
            if (_watermark.Size == WatermarkSize.Scaled)
            {
                scaledWidth = (int)Math.Round(_watermark.WidthPercent / 100.0 * frameSize.Width);
                double aspectRatio = (double)sourceHeight / sourceWidth;
                scaledHeight = (int)(scaledWidth * aspectRatio);
            }

            (int horizontalMargin, int verticalMargin) = _watermark.PlaceWithinSourceContent
                ? SourceContentMargins(squarePixelFrameSize, frameSize)
                : NormalMargins(frameSize);

            var location = CalculatePosition(
                _watermark.Location,
                frameSize.Width,
                frameSize.Height,
                scaledWidth,
                scaledHeight,
                horizontalMargin,
                verticalMargin);
            _location = new SKPointI(location.X, location.Y);

            _animatedDurationMs = 0;

            var scaledImageInfo = new SKImageInfo(scaledWidth, scaledHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

            for (var i = 0; i < _sourceCodec.FrameCount; i++)
            {
                _sourceCodec.GetFrameInfo(i, out var frameInfo);
                int frameDuration = frameInfo.Duration;
                if (frameDuration == 0)
                {
                    frameDuration = 100;
                }

                using var frameBitmap = new SKBitmap(_sourceCodec.Info);
                var pointer = frameBitmap.GetPixels();
                _sourceCodec.GetPixels(_sourceCodec.Info, pointer, new SKCodecOptions(i));

                var scaledBitmap = new SKBitmap(scaledImageInfo);
                frameBitmap.ScalePixels(scaledBitmap, SKSamplingOptions.Default);
                _scaledFrames.Add(scaledBitmap);

                _animatedDurationMs += frameDuration;
                _frameDelays.Add(frameDuration);
            }

            if (_sourceCodec.FrameCount > 0 && _animatedDurationMs == 0)
            {
                _animatedDurationMs = int.MaxValue;
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

        SKBitmap frameForTimestamp = GetFrameForTimestamp(contentTime);
        return ValueTask.FromResult(Optional(new PreparedElementImage(frameForTimestamp, _location, opacity, false)));
    }

    private SKBitmap GetFrameForTimestamp(TimeSpan timestamp)
    {
        if (_scaledFrames.Count <= 1)
        {
            return _scaledFrames[0];
        }

        long currentTimeMs = (long)timestamp.TotalMilliseconds % _animatedDurationMs;

        long frameTime = 0;
        for (var i = 0; i < _sourceCodec.FrameCount; i++)
        {
            frameTime += _frameDelays[i];
            if (currentTimeMs <= frameTime)
            {
                return _scaledFrames[i];
            }
        }

        return _scaledFrames.Last();
    }

    internal static SKPointI CalculatePosition(
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

    private WatermarkMargins NormalMargins(Resolution frameSize)
    {
        double horizontalMargin = Math.Round(_watermark.HorizontalMarginPercent / 100.0 * frameSize.Width);
        double verticalMargin = Math.Round(_watermark.VerticalMarginPercent / 100.0 * frameSize.Height);

        return new WatermarkMargins((int)Math.Round(horizontalMargin), (int)Math.Round(verticalMargin));
    }

    private WatermarkMargins SourceContentMargins(Resolution squarePixelFrameSize, Resolution frameSize)
    {
        int horizontalPadding = frameSize.Width - squarePixelFrameSize.Width;
        int verticalPadding = frameSize.Height - squarePixelFrameSize.Height;

        double horizontalMargin = Math.Round(
            _watermark.HorizontalMarginPercent / 100.0 * squarePixelFrameSize.Width
            + horizontalPadding / 2.0);
        double verticalMargin = Math.Round(
            _watermark.VerticalMarginPercent / 100.0 * squarePixelFrameSize.Height
            + verticalPadding / 2.0);

        return new WatermarkMargins((int)Math.Round(horizontalMargin), (int)Math.Round(verticalMargin));
    }

    private sealed record WatermarkMargins(int HorizontalMargin, int VerticalMargin);

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sourceCodec?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }
}
