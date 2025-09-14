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
    public static readonly FFmpegKnownDecoder Libdav1d = new("libdav1d");
    public static readonly FFmpegKnownDecoder Libaomav1 = new("libaom-av1");
    public static readonly FFmpegKnownDecoder H264V4l2m2m = new("h264_v4l2m2m");
    public static readonly FFmpegKnownDecoder HevcV4l2m2m = new("hevc_v4l2m2m");
    public static readonly FFmpegKnownDecoder Mpeg2V4l2m2m = new("mpeg2_v4l2m2m");
    public static readonly FFmpegKnownDecoder Mpeg4V4l2m2m = new("mpeg4_v4l2m2m");
    public static readonly FFmpegKnownDecoder Vc1V4l2m2m = new("vc1_v4l2m2m");
    public static readonly FFmpegKnownDecoder Vp84V4l2m2m = new("vp8_v4l2m2m");
    public static readonly FFmpegKnownDecoder Vp94V4l2m2m = new("vp9_v4l2m2m");

    private FFmpegKnownDecoder(string Name) => this.Name = Name;

    public string Name { get; }

    public static IList<string> AllDecoders =>
        [
            Av1Cuvid.Name,
            H264Cuvid.Name,
            HevcCuvid.Name,
            Mpeg2Cuvid.Name,
            Mpeg4Cuvid.Name,
            Vc1Cuvid.Name,
            Vp9Cuvid.Name,

            Libdav1d.Name,
            Libaomav1.Name,

            H264V4l2m2m.Name,
            HevcV4l2m2m.Name,
            Mpeg2V4l2m2m.Name,
            Mpeg4V4l2m2m.Name,
            Vc1V4l2m2m.Name,
            Vp84V4l2m2m.Name,
            Vp94V4l2m2m.Name
        ];

    public static IList<string> V4l2m2mDecoders =>
        [
            H264V4l2m2m.Name,
            HevcV4l2m2m.Name,
            Mpeg2V4l2m2m.Name,
            Mpeg4V4l2m2m.Name,
            Vc1V4l2m2m.Name,
            Vp84V4l2m2m.Name,
            Vp94V4l2m2m.Name
        ];
}
