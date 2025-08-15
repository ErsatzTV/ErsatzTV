using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using Microsoft.Extensions.Logging;
using NCalc;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics.Image;

public class ImageElement(ImageGraphicsElement imageGraphicsElement, ILogger logger) : GraphicsElement, IDisposable
{
    private readonly List<SKBitmap> _scaledFrames = [];
    private readonly List<int> _frameDelays = [];

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private int _animatedDurationMs;
    private SKCodec _sourceCodec;
    private SKPointI _location;

    public override async Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(imageGraphicsElement.OpacityExpression))
            {
                var expression = new Expression(imageGraphicsElement.OpacityExpression);
                expression.EvaluateFunction += OpacityExpressionHelper.EvaluateFunction;
                _maybeOpacityExpression = expression;
            }
            else
            {
                _opacity = (imageGraphicsElement.OpacityPercent ?? 100) / 100.0f;
            }

            ZIndex = imageGraphicsElement.ZIndex ?? 0;

            foreach (var expression in _maybeOpacityExpression)
            {
                expression.EvaluateFunction += OpacityExpressionHelper.EvaluateFunction;
            }

            Stream imageStream;
            bool isRemoteUri = Uri.TryCreate(imageGraphicsElement.Image, UriKind.Absolute, out var uriResult)
                               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isRemoteUri)
            {
                using var client = new HttpClient();
                imageStream = new MemoryStream(await client.GetByteArrayAsync(uriResult, cancellationToken));
            }
            else
            {
                imageStream = new FileStream(imageGraphicsElement.Image!, FileMode.Open, FileAccess.Read);
            }

            _sourceCodec = SKCodec.Create(imageStream);

            int sourceWidth = _sourceCodec.Info.Width;
            int sourceHeight = _sourceCodec.Info.Height;

            int scaledWidth = sourceWidth;
            int scaledHeight = sourceHeight;
            if (imageGraphicsElement.Scale)
            {
                scaledWidth = (int)Math.Round((imageGraphicsElement.ScaleWidthPercent ?? 100) / 100.0 * frameSize.Width);
                double aspectRatio = (double)sourceHeight / sourceWidth;
                scaledHeight = (int)(scaledWidth * aspectRatio);
            }

            int horizontalMargin = (int)Math.Round((imageGraphicsElement.HorizontalMarginPercent ?? 0) / 100.0 * frameSize.Width);
            int verticalMargin = (int)Math.Round((imageGraphicsElement.VerticalMarginPercent ?? 0) / 100.0 * frameSize.Height);

            _location = CalculatePosition(
                imageGraphicsElement.Location,
                frameSize.Width,
                frameSize.Height,
                scaledWidth,
                scaledHeight,
                horizontalMargin,
                verticalMargin);

            _animatedDurationMs = 0;

            var scaledImageInfo = new SKImageInfo(scaledWidth, scaledHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

            if (_sourceCodec.FrameCount == 0)
            {
                // static image
                using var frameBitmap = SKBitmap.Decode(_sourceCodec);
                if (frameBitmap != null)
                {
                    var scaledBitmap = new SKBitmap(scaledImageInfo);
                    frameBitmap.ScalePixels(scaledBitmap, SKSamplingOptions.Default);
                    _scaledFrames.Add(scaledBitmap);
                }
            }
            else
            {
                // animated image
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
            }

            if (_sourceCodec.FrameCount > 0 && _animatedDurationMs == 0)
            {
                _animatedDurationMs = int.MaxValue;
            }
        }
        catch (Exception ex)
        {
            IsFailed = true;
            logger.LogWarning(ex, "Failed to initialize image element; will disable for this content");
        }
    }

    public override ValueTask<Option<PreparedElementImage>> PrepareImage(
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sourceCodec?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }
}
