namespace ErsatzTV.FFmpeg;

public record ColorParams(string ColorRange, string ColorSpace, string ColorTransfer, string ColorPrimaries)
{
    public static readonly ColorParams Default = new("tv", "bt709", "bt709", "bt709");

    public bool IsHdr => ColorTransfer is "arib-std-b67" or "smpte2084";

    public bool IsUnknown => string.IsNullOrWhiteSpace(ColorSpace) &&
                             string.IsNullOrWhiteSpace(ColorTransfer) &&
                             string.IsNullOrWhiteSpace(ColorPrimaries);

    public bool IsBt709 => this == Default;
}
