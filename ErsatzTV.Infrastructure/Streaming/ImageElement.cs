using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using Microsoft.Extensions.Logging;
using NCalc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming;

public class ImageElement(ImageGraphicsElement imageGraphicsElement, ILogger logger) : IGraphicsElement, IDisposable
{
    private readonly List<Image> _scaledFrames = [];
    private readonly List<double> _frameDelays = [];

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private double _animatedDurationSeconds;
    private Image _sourceImage;
    private Point _location;

    public int ZIndex { get; private set; }

    public bool IsFailed { get; set; }

    public async Task InitializeAsync(
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

            bool isRemoteUri = Uri.TryCreate(imageGraphicsElement.Image, UriKind.Absolute, out var uriResult)
                               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (isRemoteUri)
            {
                using var client = new HttpClient();
                await using Stream imageStream = await client.GetStreamAsync(uriResult, cancellationToken);
                _sourceImage = await Image.LoadAsync(imageStream, cancellationToken);
            }
            else
            {
                _sourceImage = await Image.LoadAsync(imageGraphicsElement.Image!, cancellationToken);
            }

            int scaledWidth = _sourceImage.Width;
            int scaledHeight = _sourceImage.Height;
            if (imageGraphicsElement.Scale)
            {
                scaledWidth = (int)Math.Round((imageGraphicsElement.ScaleWidthPercent ?? 100) / 100.0 * frameSize.Width);
                double aspectRatio = (double)_sourceImage.Height / _sourceImage.Width;
                scaledHeight = (int)(scaledWidth * aspectRatio);
            }

            int horizontalMargin = (int)Math.Round((imageGraphicsElement.HorizontalMarginPercent ?? 0) / 100.0 * frameSize.Width);
            int verticalMargin = (int)Math.Round((imageGraphicsElement.VerticalMarginPercent ?? 0) / 100.0 * frameSize.Height);

            _location = WatermarkElement.CalculatePosition(
                imageGraphicsElement.Location,
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
            logger.LogWarning(ex, "Failed to initialize image element; will disable for this content");
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sourceImage?.Dispose();
        _scaledFrames?.ForEach(f => f.Dispose());
    }
}