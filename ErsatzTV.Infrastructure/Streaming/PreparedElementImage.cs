using SixLabors.ImageSharp;
using Image=SixLabors.ImageSharp.Image;

namespace ErsatzTV.Infrastructure.Streaming;

public record PreparedElementImage(Image Image, Point Point, float Opacity, bool Dispose);