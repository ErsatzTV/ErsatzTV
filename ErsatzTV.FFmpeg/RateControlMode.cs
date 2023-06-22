using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.FFmpeg;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum RateControlMode
{
    CBR,
    CQP,
    VBR
}
