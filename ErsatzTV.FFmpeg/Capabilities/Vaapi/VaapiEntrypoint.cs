namespace ErsatzTV.FFmpeg.Capabilities.Vaapi;

public class VaapiEntrypoint
{
    public const string Decode = "VAEntrypointVLD";
    public const string Encode = "VAEntrypointEncSlice";
    public const string EncodeLowPower = "VAEntrypointEncSliceLP";
}
