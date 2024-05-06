using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Amf;

public class EncoderH264Amf(Option<string> maybeVideoProfile) : EncoderBase
{
    public override string Name => "h264_amf";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            foreach (string videoProfile in maybeVideoProfile)
            {
                return
                [
                    "-c:v", Name,
                    "-profile:v", videoProfile.ToLowerInvariant()
                ];
            }

            return base.OutputOptions;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
