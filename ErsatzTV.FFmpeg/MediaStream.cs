using System.ComponentModel.DataAnnotations;
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
    FrameSize FrameSize,
    string SampleAspectRatio,
    string DisplayAspectRatio,
    Option<string> FrameRate,
    bool StillImage) : MediaStream(
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

    // TODO: figure out what's really causing this
    public bool IsAnamorphicEdgeCase
    {
        get
        {
            try
            {
                string[] split = SampleAspectRatio.Split(':');
                var num = double.Parse(split[0]);
                var den = double.Parse(split[1]);
                
                if (num <= den)
                {
                    return false;
                }

                double sar = num / den;
                double res = (double)FrameSize.Width / FrameSize.Height;
                return res - sar > 0.01;
            }
            catch
            {
                return false;
            }
        }
    }
}
