using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.Rkmpp;

public class EncoderHevcRkmpp : EncoderBase
{
    private readonly int _desiredBitDepth;

    public EncoderHevcRkmpp(int desiredBitDepth) => _desiredBitDepth = desiredBitDepth;

    public override string Name => "hevc_rkmpp";
    public override StreamKind Kind => StreamKind.Video;

    public override string[] OutputOptions => base.OutputOptions.Concat(
        new[]
        {
            "-profile:v",
            _desiredBitDepth == 10 ? "main10" : "main"
        }).ToArray();

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}