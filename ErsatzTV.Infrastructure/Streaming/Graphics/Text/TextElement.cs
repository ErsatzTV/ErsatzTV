using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Infrastructure.Streaming.Graphics.Fonts;
using Microsoft.Extensions.Logging;
using NCalc;
using Topten.RichTextKit;
using Scriban;
using Scriban.Runtime;
using SkiaSharp;
using RichTextKit=Topten.RichTextKit;

namespace ErsatzTV.Infrastructure.Streaming.Graphics.Text;

public class TextElement(
    TemplateFunctions templateFunctions,
    TextGraphicsElement textElement,
    Dictionary<string, object> variables,
    ILogger logger)
    : GraphicsElement, IDisposable
{
    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;
    private SKBitmap _image;
    private SKPointI _location;

    public override async Task InitializeAsync(
        Resolution squarePixelFrameSize,
        Resolution frameSize,
        int frameRate,
        CancellationToken cancellationToken)
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
                _opacity = (textElement.OpacityPercent ?? 100) / 100.0f;
            }

            ZIndex = textElement.ZIndex ?? 0;

            var scriptObject = new ScriptObject();
            scriptObject.Import(variables, renamer: member => member.Name);
            scriptObject.Import("convert_timezone", templateFunctions.ConvertTimeZone);
            scriptObject.Import("format_datetime", templateFunctions.FormatDateTime);

            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);
            string textToRender = await Template.Parse(textElement.Text).RenderAsync(context);

            var textBlock = new TextBlock { FontMapper = GraphicsEngineFonts.Mapper };
            var style = new RichTextKit.Style
            {
                FontFamily = textElement.FontFamily,
                FontSize = textElement.FontSize ?? 48,
                // FontWeight = (textElement.FontWeight ?? 400),
                // FontItalic = (textElement.FontStyle == FontStyle.Italic),
                TextColor = SKColor.TryParse(textElement.FontColor, out SKColor parsedColor)
                    ? parsedColor
                    : SKColors.White,
                // BackgroundColor = SKColor.TryParse(textElement.BackgroundColor, out SKColor parsedBackColor)
                //     ? parsedBackColor
                //     : SKColors.Transparent,
                // Underline = (textElement.TextDecoration == TextDecoration.Underline)
                //     ? UnderlineStyle.Solid
                //     : UnderlineStyle.None
                LetterSpacing = 10
            };

            textBlock.AddText(textToRender, style);

            _image = new SKBitmap((int)Math.Ceiling(textBlock.MeasuredWidth), (int)Math.Ceiling(textBlock.MeasuredHeight));
            using (var canvas = new SKCanvas(_image))
            {
                canvas.Clear(SKColors.Transparent);
                textBlock.Paint(canvas, new SKPoint(0, 0));
            }

            int horizontalMargin =
                (int)Math.Round((textElement.HorizontalMarginPercent ?? 0) / 100.0 * frameSize.Width);
            int verticalMargin = (int)Math.Round((textElement.VerticalMarginPercent ?? 0) / 100.0 * frameSize.Height);

            _location = CalculatePosition(
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
