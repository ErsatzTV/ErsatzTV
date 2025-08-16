using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using NCalc;
using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public class WatermarkElement : ImageElementBase
{
    private readonly ILogger _logger;
    private readonly string _imagePath;
    private readonly ChannelWatermark _watermark;

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;

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

    public override async Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken)
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
            else if (_watermark.Mode is ChannelWatermarkMode.OpacityExpression &&
                     !string.IsNullOrWhiteSpace(_watermark.OpacityExpression))
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

            await LoadImage(
                squarePixelFrameSize,
                frameSize,
                _imagePath,
                _watermark.Location,
                _watermark.Size == WatermarkSize.Scaled,
                _watermark.WidthPercent,
                _watermark.HorizontalMarginPercent,
                _watermark.VerticalMarginPercent,
                _watermark.PlaceWithinSourceContent,
                cancellationToken);
        }
        catch (Exception ex)
        {
            IsFailed = true;
            _logger.LogWarning(ex, "Failed to initialize watermark element; will disable for this content");
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
        return ValueTask.FromResult(Optional(new PreparedElementImage(frameForTimestamp, Location, opacity, false)));
    }
}
