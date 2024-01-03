namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownDecoder
{
    public static readonly FFmpegKnownDecoder Av1Cuvid = new("av1_cuvid");
    public static readonly FFmpegKnownDecoder H264Cuvid = new("h264_cuvid");
    public static readonly FFmpegKnownDecoder HevcCuvid = new("hevc_cuvid");
    public static readonly FFmpegKnownDecoder Mpeg2Cuvid = new("mpeg2_cuvid");
    public static readonly FFmpegKnownDecoder Mpeg4Cuvid = new("mpeg4_cuvid");
    public static readonly FFmpegKnownDecoder Vc1Cuvid = new("vc1_cuvid");
    public static readonly FFmpegKnownDecoder Vp9Cuvid = new("vp9_cuvid");

    private FFmpegKnownDecoder(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllDecoders =>
        new[]
        {
            Av1Cuvid.Name,
            H264Cuvid.Name,
            HevcCuvid.Name,
            Mpeg2Cuvid.Name,
            Mpeg4Cuvid.Name,
            Vc1Cuvid.Name,
            Vp9Cuvid.Name
        };
}
