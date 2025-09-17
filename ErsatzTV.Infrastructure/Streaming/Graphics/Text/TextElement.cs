using System.Text.RegularExpressions;
using ErsatzTV.Core.Graphics;
using ErsatzTV.Core.Interfaces.Streaming;
using Microsoft.Extensions.Logging;
using NCalc;
using SkiaSharp;
using RichTextKit = Topten.RichTextKit;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public partial class TextElement(
    GraphicsEngineFonts graphicsEngineFonts,
    TextGraphicsElement textElement,
    ILogger logger)
    : GraphicsElement, IDisposable
{
    private static readonly Regex StylePattern = StyleRegex();
    private SKBitmap _image;
    private SKPointI _location;

    private Option<Expression> _maybeOpacityExpression;
    private float _opacity;

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _image?.Dispose();
        _image = null;
    }

    public override Task InitializeAsync(GraphicsEngineContext context, CancellationToken cancellationToken)
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

            if (!string.IsNullOrWhiteSpace(textElement.IncludeFontsFrom))
            {
                if (Directory.Exists(textElement.IncludeFontsFrom))
                {
                    graphicsEngineFonts.LoadFonts(textElement.IncludeFontsFrom);
                }
                else
                {
                    logger.LogWarning(
                        "include_fonts_from path {Directory} does not exist",
                        textElement.IncludeFontsFrom);
                }
            }

            RichTextKit.TextBlock textBlock = BuildTextBlock(textElement.Text);

            _image = new SKBitmap(
                (int)Math.Ceiling(textBlock.MeasuredWidth),
                (int)Math.Ceiling(textBlock.MeasuredHeight));
            using (var canvas = new SKCanvas(_image))
            {
                canvas.Clear(SKColors.Transparent);
                textBlock.Paint(canvas, new SKPoint(0, 0));
            }

            var horizontalMargin =
                (int)Math.Round((textElement.HorizontalMarginPercent ?? 0) / 100.0 * context.FrameSize.Width);
            var verticalMargin =
                (int)Math.Round((textElement.VerticalMarginPercent ?? 0) / 100.0 * context.FrameSize.Height);

            _location = CalculatePosition(
                textElement.Location,
                context.FrameSize.Width,
                context.FrameSize.Height,
                _image.Width,
                _image.Height,
                horizontalMargin,
                verticalMargin);
        }
        catch (Exception ex)
        {
            IsFinished = true;
            logger.LogWarning(ex, "Failed to initialize text element; will disable for this content");
        }

        return Task.CompletedTask;
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

        return opacity == 0
            ? ValueTask.FromResult(Option<PreparedElementImage>.None)
            : new ValueTask<Option<PreparedElementImage>>(new PreparedElementImage(_image, _location, opacity, false));
    }

    private RichTextKit.TextBlock BuildTextBlock(string textToRender)
    {
        var textBlock = new RichTextKit.TextBlock { FontMapper = graphicsEngineFonts.Mapper };

        (Dictionary<string, RichTextKit.Style> styles, RichTextKit.Style baseStyle) = BuildTextStyles();

        var lastIndex = 0;
        foreach (Match match in StylePattern.Matches(textToRender))
        {
            // unstyled text before match
            if (match.Index > lastIndex)
            {
                textBlock.AddText(textToRender.AsSpan(lastIndex, match.Index - lastIndex), baseStyle);
            }

            string styleName = match.Groups[1].Value;
            string innerText = match.Groups[2].Value;

            if (styles.TryGetValue(styleName, out RichTextKit.Style style))
            {
                textBlock.AddText(innerText, style);
            }
            else
            {
                textBlock.AddText(match.Value, baseStyle);
            }

            lastIndex = match.Index + match.Length;
        }

        // unstyled text after match
        if (lastIndex < textToRender.Length)
        {
            textBlock.AddText(textToRender.AsSpan(lastIndex), baseStyle);
        }

        return textBlock;
    }

    private (Dictionary<string, RichTextKit.Style>, RichTextKit.Style) BuildTextStyles()
    {
        var styles = new Dictionary<string, RichTextKit.Style>();

        StyleDefinition baseStyleDef = textElement.Styles.Find(s => s.Name == textElement.BaseStyle);
        if (baseStyleDef == null)
        {
            throw new InvalidOperationException(
                $"The specified base_style '{textElement.BaseStyle}' was not found in the styles list.");
        }

        foreach (StyleDefinition s in textElement.Styles)
        {
            // start with base and merge in additional settings
            RichTextKit.Style finalStyle = RichTextStyleFromDef(baseStyleDef);

            finalStyle.FontFamily = s.FontFamily ?? finalStyle.FontFamily;
            finalStyle.FontItalic = s.FontItalic ?? finalStyle.FontItalic;
            finalStyle.FontSize = s.FontSize ?? finalStyle.FontSize;
            finalStyle.FontWeight = s.FontWeight ?? finalStyle.FontWeight;
            finalStyle.LetterSpacing = s.LetterSpacing ?? finalStyle.LetterSpacing;

            if (s.TextColor != null && SKColor.TryParse(s.TextColor, out SKColor parsedColor))
            {
                finalStyle.TextColor = parsedColor;
            }

            styles[s.Name] = finalStyle;
        }

        return (styles, RichTextStyleFromDef(baseStyleDef));

        RichTextKit.Style RichTextStyleFromDef(StyleDefinition def)
        {
            var style = new RichTextKit.Style
            {
                FontFamily = def.FontFamily,
                FontItalic = def.FontItalic ?? false,
                TextColor = SKColor.TryParse(def.TextColor, out SKColor color) ? color : SKColors.White
            };

            foreach (float fontSize in Optional(def.FontSize))
            {
                style.FontSize = fontSize;
            }

            foreach (int fontWeight in Optional(def.FontWeight))
            {
                style.FontWeight = fontWeight;
            }

            foreach (float letterSpacing in Optional(def.LetterSpacing))
            {
                style.LetterSpacing = letterSpacing;
            }

            return style;
        }
    }

    [GeneratedRegex(@"\[(\w+)\](.*?)\[/\1\]")]
    private static partial Regex StyleRegex();
}
