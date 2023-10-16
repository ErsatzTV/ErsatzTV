using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderHevcNvenc : EncoderBase
{
    private readonly bool _bFrames;

    public EncoderHevcNvenc(IHardwareCapabilities hardwareCapabilities)
    {
        if (hardwareCapabilities is NvidiaHardwareCapabilities nvidia)
        {
            _bFrames = nvidia.HevcBFrames;
        }
    }

    public override string Name => "hevc_nvenc";
    public override StreamKind Kind => StreamKind.Video;

    public override IList<string> OutputOptions =>
        new[] { "-c:v", "hevc_nvenc", "-tag:v", "hvc1", "-b_ref_mode", _bFrames ? "1" : "0" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
    };
}
