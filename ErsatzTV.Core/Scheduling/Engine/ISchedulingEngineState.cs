namespace ErsatzTV.Core.Scheduling.Engine;

public interface ISchedulingEngineState
{
    // state
    PlayoutBuildMode Mode { get; }
    int Seed { get; }
    DateTimeOffset CurrentTime { get; }
    DateTimeOffset Finish { get; }

    // result
    DateTimeOffset RemoveBefore { get; }
    bool ClearItems { get; }
    System.Collections.Generic.HashSet<int> HistoryToRemove { get; }
}
