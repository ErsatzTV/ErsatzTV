namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownEncoder
{
    private FFmpegKnownEncoder(string Name) => this.Name = Name;

    public string Name { get; }

    // only list the encoders that we actually check for
    public static IList<string> AllEncoders =>
        new[]
        {
            "h264_amf",
            "hevc_amf"
        };
}
