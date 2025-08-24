using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling.Engine;

public interface ISchedulingEngineState
{
    // state
    int PlayoutId { get; }
    PlayoutBuildMode Mode { get; }
    int Seed { get; }
    DateTimeOffset CurrentTime { get; }
    DateTimeOffset Finish { get; }

    // result
    Option<DateTimeOffset> RemoveBefore { get; }
    bool ClearItems { get; }
    List<PlayoutItem> AddedItems { get; }
    System.Collections.Generic.HashSet<int> HistoryToRemove { get; }
    List<PlayoutHistory> AddedHistory { get; }
}
