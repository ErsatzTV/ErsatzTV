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
    public int BitDepth => PixelFormat.Map(pf => pf.BitDepth).IfNone(8);
    
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
            double sar = GetSAR();
            bool edgeCase = IsAnamorphicEdgeCase;

            width = edgeCase
                ? FrameSize.Width
                : (int)Math.Floor(FrameSize.Width * sar);

            height = edgeCase
                ? (int)Math.Floor(FrameSize.Height * sar)
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

    private double GetSAR()
    {
        // some media servers don't provide sample aspect ratio so we have to calculate it
        if (string.IsNullOrWhiteSpace(SampleAspectRatio))
        {
            // first check for decimal DAR
            if (!double.TryParse(DisplayAspectRatio, out double dar))
            {
                // if not, assume it's a ratio
                string[] split = DisplayAspectRatio.Split(':');  
                var num = double.Parse(split[0]);
                var den = double.Parse(split[1]);
                dar = num / den;
            }

            double res = FrameSize.Width / (double)FrameSize.Height;
            return dar / res;
        }
        else
        {
            string[] split = SampleAspectRatio.Split(':');
            var num = double.Parse(split[0]);
            var den = double.Parse(split[1]);
            return num / den;
        }
    }
}
