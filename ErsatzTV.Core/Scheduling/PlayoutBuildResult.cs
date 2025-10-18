using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;

namespace ErsatzTV.Core.Scheduling;

public record PlayoutBuildResult(
    bool ClearItems,
    Option<DateTimeOffset> RemoveBefore,
    Option<DateTimeOffset> RemoveAfter,
    List<PlayoutItem> AddedItems,
    System.Collections.Generic.HashSet<int> ItemsToRemove,
    List<PlayoutHistory> AddedHistory,
    System.Collections.Generic.HashSet<int> HistoryToRemove,
    List<RerunHistory> AddedRerunHistory,
    System.Collections.Generic.HashSet<int> RerunHistoryToRemove,
    Option<DateTimeOffset> TimeShiftTo)
{
    public static PlayoutBuildResult Empty =>
        new(
            false,
            Option<DateTimeOffset>.None,
            Option<DateTimeOffset>.None,
            [],
            [],
            [],
            [],
            [],
            [],
            Option<DateTimeOffset>.None);

    public PlayoutBuildWarnings Warnings { get; } = new();
}
