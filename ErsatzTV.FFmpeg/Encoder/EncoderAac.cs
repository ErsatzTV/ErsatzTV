using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderAac : EncoderBase
{
    public override FrameState NextState(FrameState currentState) => currentState with { AudioFormat = AudioFormat.Aac };

    public override string Name => "aac";

    public override StreamKind Kind => StreamKind.Audio;
}
