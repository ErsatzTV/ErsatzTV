using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderH264Nvenc(Option<string> maybeVideoProfile) : EncoderBase
{
    public override string Name => "h264_nvenc";
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
                    "-preset", "llhq",
                    "-profile:v", videoProfile.ToLowerInvariant(),
                ];
            }

            return base.OutputOptions;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.H264
    };
}
