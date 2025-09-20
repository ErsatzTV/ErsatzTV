using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Nvenc;

public class EncoderHevcNvenc : EncoderBase
{
    private readonly bool _bFrames;
    private readonly Option<string> _maybeVideoPreset;
    private readonly bool _allowBFrames;

    public EncoderHevcNvenc(IHardwareCapabilities hardwareCapabilities, Option<string> maybeVideoPreset, bool allowBFrames)
    {
        _maybeVideoPreset = maybeVideoPreset;
        _allowBFrames = allowBFrames;
        if (hardwareCapabilities is NvidiaHardwareCapabilities nvidia)
        {
            _bFrames = nvidia.HevcBFrames;
        }
    }

    public override string Name => "hevc_nvenc";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions
    {
        get
        {
            var result = new List<string>
            {
                "-c:v", "hevc_nvenc",
                "-tag:v", "hvc1",
                "-b_ref_mode", _allowBFrames && _bFrames ? "1" : "0"
            };

            foreach (string videoPreset in _maybeVideoPreset)
            {
                if (!string.IsNullOrWhiteSpace(videoPreset))
                {
                    result.Add("-preset:v");
                    result.Add(videoPreset);
                }
            }

            return result.ToArray();
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc
    };
}
