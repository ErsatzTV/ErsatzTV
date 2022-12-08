using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder.VideoToolbox;

public class EncoderHevcVideoToolbox : EncoderBase
{
    private readonly int _desiredBitDepth;

    public EncoderHevcVideoToolbox(int desiredBitDepth) => _desiredBitDepth = desiredBitDepth;

    public override string Name => "hevc_videotoolbox";
    public override StreamKind Kind => StreamKind.Video;

    public override IList<string> OutputOptions => base.OutputOptions.Concat(
        new List<string>
        {
            "-profile:v",
            _desiredBitDepth == 10 ? "main10" : "main"
        }).ToList();

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoFormat = VideoFormat.Hevc,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
