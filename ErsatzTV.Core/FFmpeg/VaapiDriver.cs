using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.FFmpeg
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VaapiDriver
    {
        Default = 0,
        iHD = 1,
        i965 = 2,
        RadeonSI = 3
    }
}
