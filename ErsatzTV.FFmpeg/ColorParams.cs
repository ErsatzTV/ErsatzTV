namespace ErsatzTV.FFmpeg;

public record ColorParams(string ColorRange, string ColorSpace, string ColorTransfer, string ColorPrimaries)
{
    public static readonly ColorParams Default = new("tv", "bt709", "bt709", "bt709");
    public static readonly ColorParams Bt470Bg = new("tv", "bt470bg", "bt470bg", "bt470bg");
    public static readonly ColorParams Unknown = new("tv", string.Empty, string.Empty, string.Empty);

    public bool IsHdr => ColorTransfer is "arib-std-b67" or "smpte2084";

    public bool IsUnknown => string.IsNullOrWhiteSpace(ColorSpace) &&
                             string.IsNullOrWhiteSpace(ColorTransfer) &&
                             string.IsNullOrWhiteSpace(ColorPrimaries);

    public bool IsMixed => ColorSpace != ColorTransfer || ColorTransfer != ColorPrimaries ||
                           string.IsNullOrWhiteSpace(ColorRange);

    public bool IsBt709
    {
        get
        {
            if (this == Default)
            {
                return true;
            }

            // some sources don't set transfer and primaries metadata
            return ColorRange == "tv" && ColorSpace == "bt709"
                                      && string.IsNullOrWhiteSpace(ColorTransfer)
                                      && string.IsNullOrWhiteSpace(ColorPrimaries);
        }
    }
}
