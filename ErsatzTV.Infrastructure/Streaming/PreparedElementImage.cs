using SkiaSharp;

namespace ErsatzTV.Infrastructure.Streaming;

public record PreparedElementImage(SKBitmap Image, SKPointI Point, float Opacity, bool Dispose);