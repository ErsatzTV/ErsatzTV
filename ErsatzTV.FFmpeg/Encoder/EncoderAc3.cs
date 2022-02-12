using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderAc3 : EncoderBase
{
    public override FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public override FrameState NextState(FrameState currentState) => currentState with { AudioFormat = AudioFormat.Ac3 };

    public override string Name => "ac3";

    public override StreamKind Kind => StreamKind.Audio;
}
