namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownEncoder
{
    public static readonly FFmpegKnownEncoder H264VideoToolbox = new("h264_videotoolbox");
    public static readonly FFmpegKnownEncoder HevcVideoToolbox = new("hevc_videotoolbox");
    private FFmpegKnownEncoder(string Name) => this.Name = Name;

    public string Name { get; }

    // only list the encoders that we actually check for
    public static IList<string> AllEncoders =>
    [
        "h264_amf",
        "hevc_amf",
        "h264_v4l2m2m",
        "hevc_v4l2m2m",
        "h264_videotoolbox",
        "hevc_videotoolbox"
    ];
}
