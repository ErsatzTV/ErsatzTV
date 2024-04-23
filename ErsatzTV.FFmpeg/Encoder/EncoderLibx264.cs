using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderLibx264(Option<string> maybeVideoProfile) : EncoderBase
{
    public override string Name => "libx264";
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

    public override FrameState NextState(FrameState currentState) =>
        currentState with { VideoFormat = VideoFormat.H264 };
}
