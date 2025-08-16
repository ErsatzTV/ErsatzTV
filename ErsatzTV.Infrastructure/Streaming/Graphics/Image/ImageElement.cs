using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using Microsoft.Extensions.Logging;
using NCalc;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class ImageElement(ImageGraphicsElement imageGraphicsElement, ILogger logger) : ImageElementBase
{
    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;

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

            foreach (Expression expression in _maybeOpacityExpression)
            {
                expression.EvaluateFunction += OpacityExpressionHelper.EvaluateFunction;
            }

            await LoadImage(
                squarePixelFrameSize,
                frameSize,
                imageGraphicsElement.Image,
                imageGraphicsElement.Location,
                imageGraphicsElement.Scale,
                imageGraphicsElement.ScaleWidthPercent,
                imageGraphicsElement.HorizontalMarginPercent,
                imageGraphicsElement.VerticalMarginPercent,
                false,
                cancellationToken);
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
        foreach (Expression expression in _maybeOpacityExpression)
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
        return ValueTask.FromResult(Optional(new PreparedElementImage(frameForTimestamp, Location, opacity, false)));
    }
}
