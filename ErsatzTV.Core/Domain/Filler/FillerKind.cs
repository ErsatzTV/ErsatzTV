namespace ErsatzTV.Core.Domain.Filler;

public enum FillerKind
{
    None = 0,
    PreRoll = 1,
    MidRollEnter = 2,
    MidRoll = 3,
    MidRollExit = 4,
    PostRoll = 5,
    Tail = 6,
    Fallback = 7,
    
    GuideMode = 99
}
