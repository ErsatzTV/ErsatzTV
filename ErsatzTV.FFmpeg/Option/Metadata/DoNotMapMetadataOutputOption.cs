namespace ErsatzTV.FFmpeg.Option.Metadata;

public class DoNotMapMetadataOutputOption : OutputOption
{
    public override IList<string> OutputOptions => new List<string> { "-map_metadata", "-1" };

    public override FrameState NextState(FrameState currentState) => currentState with { DoNotMapMetadata = true };
}
