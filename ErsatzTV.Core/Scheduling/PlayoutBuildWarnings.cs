namespace ErsatzTV.Core.Scheduling;

public class PlayoutBuildWarnings
{
    public void Merge(PlayoutBuildWarnings warnings)
    {
        TailFillerTooLong += warnings.TailFillerTooLong;
        MidRollContentWithoutChapters += warnings.MidRollContentWithoutChapters;
        DurationFillerSkipped += warnings.DurationFillerSkipped;

        BlockItemSkippedEmptyCollection += warnings.BlockItemSkippedEmptyCollection;
    }

    public int TailFillerTooLong { get; set; }
    public int MidRollContentWithoutChapters { get; set; }
    public int DurationFillerSkipped { get; set; }

    public int BlockItemSkippedEmptyCollection { get; set; }
}
