using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Images;
using static System.Net.Mime.MediaTypeNames;
using SkiaSharp;

namespace ErsatzTV.Core.Images;

public class ChannelLogoGenerator : IChannelLogoGenerator
{
    public Either<BaseError, byte[]> GenerateChannelLogo(
        string text,
        int logoHeight,
        int logoWidth,
        CancellationToken cancellationToken)
    {
        using (var surface = SKSurface.Create(new SKImageInfo(logoWidth, logoHeight)))
        {
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Black);

            //etv logo
            string overlayImagePath = Path.Combine("wwwroot", "images", "ersatztv-500.png");
            using (SKBitmap overlayImage = SKBitmap.Decode(overlayImagePath))
            {
                canvas.DrawBitmap(overlayImage, new SKRect(155, 60, 205, 110));
            }

            //Custom Font
            string fontPath = Path.Combine(FileSystemLayout.ResourcesCacheFolder, "Sen.ttf");
            using (var fontTypeface = SKTypeface.FromFile(fontPath))
            {
                var fontSize = 30f;
                var paint = new SKPaint
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
                float x = logoWidth / 2;
                float y = logoHeight / 2 - textBounds.MidY;
                canvas.DrawText(text, x, y, paint);
            }

            using (var image = surface.Snapshot())
            using (var ms = new MemoryStream())
            {
                image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }
                
    }
}
