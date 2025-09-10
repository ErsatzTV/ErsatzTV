namespace ErsatzTV.Core.Domain.Filler;

public enum FillerKind
{
    Unscheduled = -1,
    None = 0,
    PreRoll = 1,
    MidRoll = 2,
    PostRoll = 3,
    Tail = 4,
    Fallback = 5,

    GuideMode = 99,

    DecoDefault = 100
}
