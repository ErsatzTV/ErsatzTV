namespace ErsatzTV.FFmpeg.Capabilities;

public static class FourCC
{
    public const string Av1 = "av01";
    public const string H264 = "avc1";
    public const string Hevc = "hvc1";
    public const string Mpeg2Video = "mp2v";
    public const string Mpeg4 = "mp4v";
    public const string Vp9 = "vp09";

    public static readonly List<string> AllVideoToolbox =
    [
        Av1,
        H264,
        Hevc,
        Mpeg2Video,
        Mpeg4,
        Vp9
    ];

    public static readonly List<string> AllRkmpp =
    [
        Av1,
        H264,
        Hevc,
        Mpeg2Video,
        Mpeg4,
        Vp9
    ];
}
