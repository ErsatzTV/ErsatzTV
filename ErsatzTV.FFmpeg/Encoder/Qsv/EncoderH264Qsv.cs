using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Qsv;

public class EncoderH264Qsv(Option<string> maybeVideoProfile) : EncoderBase
{
    public override string Name => "h264_qsv";
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
                    "-low_power", "0",
                    "-look_ahead", "0",
                    "-profile:v", videoProfile.ToLowerInvariant(),
                ];
            }

            return ["-c:v", Name, "-low_power", "0", "-look_ahead", "0"];
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
