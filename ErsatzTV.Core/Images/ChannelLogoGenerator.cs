using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Images;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ErsatzTV.Core.Images;

public class ChannelLogoGenerator : IChannelLogoGenerator
{
    public const string GetRoute = "/iptv/logos/gen";
    public const string GetRouteQueryParamName = "text";

    private readonly ILogger _logger;

    public ChannelLogoGenerator(
            ILogger<ChannelLogoGenerator> logger)
    {
        _logger = logger;
    }

    public static Option<string> GenerateChannelLogoUrl(Channel channel) =>
         $"http://localhost:{Settings.ListenPort}{GetRoute}?{GetRouteQueryParamName}={channel.WebEncodedName}";

    public Either<BaseError, byte[]> GenerateChannelLogo(
        string text,
        int logoHeight,
        int logoWidth,
        CancellationToken cancellationToken)
    {
        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(logoWidth, logoHeight));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            //etv logo
            string overlayImagePath = Path.Combine("wwwroot", "images", "ersatztv-500.png");
            using SKBitmap overlayImage = SKBitmap.Decode(overlayImagePath);
            canvas.DrawBitmap(overlayImage, new SKRect(155, 60, 205, 110));

            //Custom Font
            string fontPath = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "Sen.ttf");
            using SKTypeface fontTypeface = SKTypeface.FromFile(fontPath);
            int fontSize = 30;
            SKPaint paint = new SKPaint
            {
                Typeface = fontTypeface,
                TextSize = fontSize,
                IsAntialias = true,
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center
            };

            SKRect textBounds = new SKRect();
            paint.MeasureText(text, ref textBounds);

            // Ajuster la taille de la police si nécessaire
            while (textBounds.Width > logoWidth - 10 && fontSize > 16)
            {
                fontSize -= 2;
                paint.TextSize = fontSize;
                paint.MeasureText(text, ref textBounds);
            }

            // Dessiner le texte
            float x = logoWidth / 2f;
            float y = logoHeight / 2f - textBounds.MidY;
            canvas.DrawText(text, x, y, paint);

            using SKImage image = surface.Snapshot();
            using MemoryStream ms = new MemoryStream();
            image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError("Can't generate Channel Logo ([{ErrorType}] {ErrorMessage})", ex.GetType(), ex.Message);
            return BaseError.New("Can't generate Channel Logo " + ex.Message);
        }
    }
}
