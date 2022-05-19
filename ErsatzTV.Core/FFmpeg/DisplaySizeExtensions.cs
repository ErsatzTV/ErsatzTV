using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public static class DisplaySizeExtensions
{
    internal static bool IsSameSizeAs(this IDisplaySize @this, IDisplaySize that) =>
        @this.Width == that.Width && @this.Height == that.Height;
}
