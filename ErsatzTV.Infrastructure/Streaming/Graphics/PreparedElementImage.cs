using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming.Graphics;

public record PreparedElementImage(SKBitmap Image, SKPointI Point, float Opacity, bool Dispose);