using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg;

public record MediaStream(int Index, string Codec, StreamKind Kind);

public record AudioStream(int Index, string Codec, int Channels) : MediaStream(
    Index,
    Codec,
    StreamKind.Audio);

public record VideoStream(
    int Index,
    string Codec,
    Option<IPixelFormat> PixelFormat,
    ColorParams ColorParams,
    FrameSize FrameSize,
    string SampleAspectRatio,
    string DisplayAspectRatio,
    Option<string> FrameRate,
    bool StillImage,
    ScanKind ScanKind) : MediaStream(
    Index,
    Codec,
    StreamKind.Video)
{
    public bool IsAnamorphic
    {
        get
        {
            // square pixels
            if (SampleAspectRatio == "1:1")
            {
                return false;
            }

            // 0:1 is "unspecified", so anything other than that will be non-square/anamorphic
            if (SampleAspectRatio != "0:1")
            {
                return true;
            }

            // SAR 0:1 && DAR 0:1 (both unspecified) means square
            if (DisplayAspectRatio == "0:1")
            {
                return false;
            }

            // DAR == W:H is square
            return DisplayAspectRatio != $"{FrameSize.Width}:{FrameSize.Height}";
        }
    }

    // TODO: figure out what's really causing this, then re-enable if needed
    public bool IsAnamorphicEdgeCase => false;

    public FrameSize SquarePixelFrameSize(FrameSize resolution)
    {
        int width = FrameSize.Width;
        int height = FrameSize.Height;

        if (IsAnamorphic)
        {
            string[] split = SampleAspectRatio.Split(':');
            var num = double.Parse(split[0]);
            var den = double.Parse(split[1]);

            bool edgeCase = IsAnamorphicEdgeCase;

            width = edgeCase
                ? FrameSize.Width
                : (int)Math.Floor(FrameSize.Width * num / den);

            height = edgeCase
                ? (int)Math.Floor(FrameSize.Height * num / den)
                : FrameSize.Height;
        }

        double widthPercent = (double)resolution.Width / width;
        double heightPercent = (double)resolution.Height / height;
        double minPercent = Math.Min(widthPercent, heightPercent);

        var result = new FrameSize(
            (int)Math.Floor(width * minPercent),
            (int)Math.Floor(height * minPercent));

        return result;
    }
}
