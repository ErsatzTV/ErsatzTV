namespace ErsatzTV.FFmpeg.Capabilities;

public static class FourCC
{
    public const string H264 = "avc1";
    public const string Hevc = "hvc1";
    public const string Vp9 = "vp90";

    public static readonly List<string> AllVideoToolbox =
    [
        H264,
        Hevc,
        Vp9
    ];
}
