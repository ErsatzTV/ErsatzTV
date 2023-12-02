namespace ErsatzTV.FFmpeg.Capabilities;

public record FFmpegKnownEncoder
{
    public string Name { get; }

    private FFmpegKnownEncoder(string Name)
    {
        this.Name = Name;
    }

    // only list the encoders that we actually check for
    public static IList<string> AllEncoders =>
        new[]
        {
            "h264_amf",
            "hevc_amf"
        };
}
