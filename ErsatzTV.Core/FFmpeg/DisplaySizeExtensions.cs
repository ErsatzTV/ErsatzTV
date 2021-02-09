using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg
{
    public static class DisplaySizeExtensions
    {
        internal static IDisplaySize PadToEven(this IDisplaySize size) =>
            new DisplaySize(size.Width + size.Width % 2, size.Height + size.Height % 2);

        internal static bool IsSameSizeAs(this IDisplaySize @this, IDisplaySize that) =>
            @this.Width == that.Width && @this.Height == that.Height;
    }
}
