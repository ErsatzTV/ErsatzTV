using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using Microsoft.Extensions.Logging;
using NCalc;
using Scriban;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image=SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming;

public class TextElement(TextGraphicsElement textElement, Dictionary<string, string> variables, ILogger logger) : IGraphicsElement, IDisposable
{
    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private Image _image;
    private Point _location;

    public int ZIndex { get; private set; }

    public bool IsFailed { get; set; }

    public async Task InitializeAsync(Resolution frameSize, int frameRate, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(textElement.OpacityExpression))
            {
                var expression = new Expression(textElement.OpacityExpression);
                expression.EvaluateFunction += OpacityExpressionHelper.EvaluateFunction;
                _maybeOpacityExpression = expression;
            }
            else
            {
                _opacity = (textElement.Opacity ?? 100) / 100.0f;
            }

            ZIndex = textElement.ZIndex ?? 0;

            string textToRender = await Template.Parse(textElement.Text).RenderAsync(variables);

            var font = GraphicsEngineFonts.GetFont(textElement.FontFamily, textElement.FontSize ?? 48, FontStyle.Regular);
            var fontColor = Color.White;
            if (Color.TryParse(textElement.FontColor, out Color parsedColor) ||
                Color.TryParseHex(textElement.FontColor, out parsedColor))
            {
                fontColor = parsedColor;
            }

            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // if (Enum.TryParse(textElement.HorizontalAlignment, out HorizontalAlignment parsedAlignment))
            // {
            //     textOptions.HorizontalAlignment = parsedAlignment;
            // }

            FontRectangle textBounds = TextMeasurer.MeasureBounds(textToRender, textOptions);
            textOptions.Origin = new PointF(-textBounds.X, -textBounds.Y);

            _image = new Image<Rgba32>((int)Math.Ceiling(textBounds.Width), (int)Math.Ceiling(textBounds.Height));
            _image.Mutate(ctx => ctx.DrawText(textOptions, textToRender, fontColor));

            int horizontalMargin = (int)Math.Round((textElement.HorizontalMarginPercent ?? 0) / 100.0 * frameSize.Width);
            int verticalMargin = (int)Math.Round((textElement.VerticalMarginPercent ?? 0) / 100.0 * frameSize.Height);

            _location = WatermarkElement.CalculatePosition(
                textElement.Location,
                frameSize.Width,
                frameSize.Height,
                _image.Width,
                _image.Height,
                horizontalMargin,
                verticalMargin);
        }
        catch (Exception ex)
        {
            IsFailed = true;
            logger.LogWarning(ex, "Failed to initialize text element; will disable for this content");
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

        return opacity == 0
            ? ValueTask.FromResult(Option<PreparedElementImage>.None)
            : new ValueTask<Option<PreparedElementImage>>(new PreparedElementImage(_image, _location, opacity, false));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _image?.Dispose();
        _image = null;
    }
}